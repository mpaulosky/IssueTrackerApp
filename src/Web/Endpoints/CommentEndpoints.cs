// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentEndpoints.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using System.Security.Claims;

using Domain.Abstractions;
using Domain.DTOs;

using Web.Services;

namespace Web.Features;

/// <summary>
/// Minimal API endpoints for Comment CRUD operations.
/// </summary>
public static class CommentEndpoints
{
	/// <summary>
	/// Maps all comment-related API endpoints.
	/// </summary>
	public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/api");

		// GET /api/issues/{issueId}/comments - List comments for an issue
		group.MapGet("/issues/{issueId}/comments", GetCommentsAsync)
			.WithName("GetComments")
			.WithDescription("Gets all comments for a specific issue")
			.Produces<IReadOnlyList<CommentDto>>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization();

		// POST /api/issues/{issueId}/comments - Add a comment to an issue
		group.MapPost("/issues/{issueId}/comments", AddCommentAsync)
			.WithName("AddComment")
			.WithDescription("Adds a new comment to an issue")
			.Produces<CommentDto>(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization();

		// PUT /api/comments/{id} - Update a comment (owner only)
		group.MapPut("/comments/{id}", UpdateCommentAsync)
			.WithName("UpdateComment")
			.WithDescription("Updates an existing comment (owner only)")
			.Produces<CommentDto>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status403Forbidden)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization();

		// DELETE /api/comments/{id} - Delete a comment (owner or admin)
		group.MapDelete("/comments/{id}", DeleteCommentAsync)
			.WithName("DeleteComment")
			.WithDescription("Deletes (archives) a comment (owner or admin only)")
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status403Forbidden)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization();

		return endpoints;
	}

	private static async Task<IResult> GetCommentsAsync(
		string issueId,
		ICommentService commentService,
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var result = await commentService.GetCommentsAsync(issueId, includeArchived, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.NotFound
				? Results.NotFound(new { error = result.Error })
				: Results.BadRequest(new { error = result.Error });
		}

		return Results.Ok(result.Value);
	}

	private static async Task<IResult> AddCommentAsync(
		string issueId,
		AddCommentRequest request,
		ICommentService commentService,
		ClaimsPrincipal user,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(request.Title))
		{
			return Results.BadRequest(new { error = "Title is required" });
		}

		if (string.IsNullOrWhiteSpace(request.Description))
		{
			return Results.BadRequest(new { error = "Description is required" });
		}

		var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
		var userName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
		var userEmail = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
		var author = new UserDto(userId, userName, userEmail);

		var result = await commentService.AddCommentAsync(
			issueId,
			request.Title,
			request.Description,
			author,
			cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.NotFound
				? Results.NotFound(new { error = result.Error })
				: Results.BadRequest(new { error = result.Error });
		}

		return Results.Created($"/api/issues/{issueId}/comments/{result.Value!.Id}", result.Value);
	}

	private static async Task<IResult> UpdateCommentAsync(
		string id,
		UpdateCommentRequest request,
		ICommentService commentService,
		ClaimsPrincipal user,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(request.Title))
		{
			return Results.BadRequest(new { error = "Title is required" });
		}

		if (string.IsNullOrWhiteSpace(request.Description))
		{
			return Results.BadRequest(new { error = "Description is required" });
		}

		var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

		var result = await commentService.UpdateCommentAsync(
			id,
			request.IssueId ?? string.Empty,
			request.Title,
			request.Description,
			userId,
			cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode switch
			{
				ResultErrorCode.NotFound => Results.NotFound(new { error = result.Error }),
				ResultErrorCode.Validation when result.Error?.Contains("author") == true
					=> Results.Forbid(),
				_ => Results.BadRequest(new { error = result.Error })
			};
		}

		return Results.Ok(result.Value);
	}

	private static async Task<IResult> DeleteCommentAsync(
		string id,
		ICommentService commentService,
		ClaimsPrincipal user,
		CancellationToken cancellationToken = default,
		string? issueId = null)
	{
		var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
		var userName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
		var userEmail = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
		var isAdmin = user.IsInRole("Admin");
		var archivedBy = new UserDto(userId, userName, userEmail);

		var result = await commentService.DeleteCommentAsync(
			id,
			issueId ?? string.Empty,
			userId,
			isAdmin,
			archivedBy,
			cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode switch
			{
				ResultErrorCode.NotFound => Results.NotFound(new { error = result.Error }),
				ResultErrorCode.Validation when result.Error?.Contains("author") == true
					=> Results.Forbid(),
				_ => Results.BadRequest(new { error = result.Error })
			};
		}

		return Results.NoContent();
	}
}

/// <summary>
/// Request model for adding a comment.
/// </summary>
public record AddCommentRequest(string Title, string Description);

/// <summary>
/// Request model for updating a comment.
/// </summary>
public record UpdateCommentRequest(string Title, string Description, string? IssueId = null);
