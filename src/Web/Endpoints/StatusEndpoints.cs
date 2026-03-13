// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusEndpoints.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Services;

namespace Web.Endpoints;

/// <summary>
///   Request model for creating a status.
/// </summary>
public record CreateStatusRequest(string StatusName, string StatusDescription);

/// <summary>
///   Request model for updating a status.
/// </summary>
public record UpdateStatusRequest(string StatusName, string StatusDescription);

/// <summary>
///   Request model for archiving a status.
/// </summary>
public record ArchiveStatusRequest(bool Archive);

/// <summary>
///   Extension methods for mapping Status API endpoints.
/// </summary>
public static class StatusEndpoints
{
	/// <summary>
	///   Maps Status API endpoints to the application.
	/// </summary>
	public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/statuses")
			.WithTags("Statuses");

		group.MapGet("/", GetAllStatuses)
			.WithName("GetAllStatuses")
			.WithSummary("Get all statuses")
			.WithDescription("Retrieves all statuses, optionally including archived ones.")
			.Produces<IEnumerable<StatusDto>>()
			.Produces(StatusCodes.Status500InternalServerError);

		group.MapGet("/{id}", GetStatusById)
			.WithName("GetStatusById")
			.WithSummary("Get a status by ID")
			.WithDescription("Retrieves a single status by its unique identifier.")
			.Produces<StatusDto>()
			.Produces(StatusCodes.Status404NotFound)
			.Produces(StatusCodes.Status400BadRequest);

		group.MapPost("/", CreateStatus)
			.WithName("CreateStatus")
			.WithSummary("Create a new status")
			.WithDescription("Creates a new status with the provided name and description.")
			.Produces<StatusDto>(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status409Conflict)
			.RequireAuthorization();

		group.MapPut("/{id}", UpdateStatus)
			.WithName("UpdateStatus")
			.WithSummary("Update an existing status")
			.WithDescription("Updates an existing status with the provided name and description.")
			.Produces<StatusDto>()
			.Produces(StatusCodes.Status404NotFound)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status409Conflict)
			.RequireAuthorization();

		group.MapDelete("/{id}", ArchiveStatus)
			.WithName("ArchiveStatus")
			.WithSummary("Archive a status")
			.WithDescription("Archives (soft deletes) a status by its unique identifier.")
			.Produces<StatusDto>()
			.Produces(StatusCodes.Status404NotFound)
			.Produces(StatusCodes.Status400BadRequest)
			.RequireAuthorization();

		return app;
	}

	/// <summary>
	///   Gets all statuses with optional filtering.
	/// </summary>
	private static async Task<IResult> GetAllStatuses(
		[FromQuery] bool includeArchived,
		IStatusService statusService,
		CancellationToken cancellationToken)
	{
		var result = await statusService.GetStatusesAsync(includeArchived, cancellationToken);

		if (result.Failure)
		{
			return Results.Problem(
				detail: result.Error,
				statusCode: StatusCodes.Status500InternalServerError);
		}

		return Results.Ok(result.Value);
	}

	/// <summary>
	///   Gets a single status by ID.
	/// </summary>
	private static async Task<IResult> GetStatusById(
		string id,
		IStatusService statusService,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return Results.BadRequest("Status ID is required.");
		}

		var result = await statusService.GetStatusByIdAsync(id, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.NotFound
				? Results.NotFound(result.Error)
				: Results.BadRequest(result.Error);
		}

		return Results.Ok(result.Value);
	}

	/// <summary>
	///   Creates a new status.
	/// </summary>
	private static async Task<IResult> CreateStatus(
		[FromBody] CreateStatusRequest request,
		IStatusService statusService,
		IValidator<Domain.Features.Statuses.Commands.CreateStatusCommand> validator,
		CancellationToken cancellationToken)
	{
		// Validate the request
		var command = new Domain.Features.Statuses.Commands.CreateStatusCommand(
			request.StatusName,
			request.StatusDescription);

		var validationResult = await validator.ValidateAsync(command, cancellationToken);

		if (!validationResult.IsValid)
		{
			var errors = validationResult.Errors
				.Select(e => new { e.PropertyName, e.ErrorMessage })
				.ToList();

			return Results.BadRequest(new { Errors = errors });
		}

		var result = await statusService.CreateStatusAsync(
			request.StatusName,
			request.StatusDescription,
			cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.Conflict
				? Results.Conflict(result.Error)
				: Results.BadRequest(result.Error);
		}

		return Results.Created($"/api/statuses/{result.Value!.Id}", result.Value);
	}

	/// <summary>
	///   Updates an existing status.
	/// </summary>
	private static async Task<IResult> UpdateStatus(
		string id,
		[FromBody] UpdateStatusRequest request,
		IStatusService statusService,
		IValidator<Domain.Features.Statuses.Commands.UpdateStatusCommand> validator,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return Results.BadRequest("Status ID is required.");
		}

		// Validate the request
		var command = new Domain.Features.Statuses.Commands.UpdateStatusCommand(
			id,
			request.StatusName,
			request.StatusDescription);

		var validationResult = await validator.ValidateAsync(command, cancellationToken);

		if (!validationResult.IsValid)
		{
			var errors = validationResult.Errors
				.Select(e => new { e.PropertyName, e.ErrorMessage })
				.ToList();

			return Results.BadRequest(new { Errors = errors });
		}

		var result = await statusService.UpdateStatusAsync(
			id,
			request.StatusName,
			request.StatusDescription,
			cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode switch
			{
				ResultErrorCode.NotFound => Results.NotFound(result.Error),
				ResultErrorCode.Conflict => Results.Conflict(result.Error),
				_ => Results.BadRequest(result.Error)
			};
		}

		return Results.Ok(result.Value);
	}

	/// <summary>
	///   Archives (soft deletes) a status.
	/// </summary>
	private static async Task<IResult> ArchiveStatus(
		string id,
		IStatusService statusService,
		HttpContext httpContext,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return Results.BadRequest("Status ID is required.");
		}

		// Get the current user info for audit trail
		var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
		var userName = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
		var userEmail = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";

		var archivedBy = new UserDto(userId, userName, userEmail);

		var result = await statusService.ArchiveStatusAsync(id, true, archivedBy, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.NotFound
				? Results.NotFound(result.Error)
				: Results.BadRequest(result.Error);
		}

		return Results.Ok(result.Value);
	}
}
