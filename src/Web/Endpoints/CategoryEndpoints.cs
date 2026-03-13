// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryEndpoints.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Web.Services;

namespace Web.Endpoints;

/// <summary>
///   Extension methods for mapping Category API endpoints.
/// </summary>
public static class CategoryEndpoints
{
	/// <summary>
	///   Maps Category API endpoints to the route builder.
	/// </summary>
	public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/categories")
			.WithTags("Categories");

		group.MapGet("/", GetAllCategories)
			.WithName("GetCategories")
			.WithDescription("Gets all categories")
			.Produces<IEnumerable<CategoryDto>>(StatusCodes.Status200OK);

		group.MapGet("/{id}", GetCategoryById)
			.WithName("GetCategoryById")
			.WithDescription("Gets a category by ID")
			.Produces<CategoryDto>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status404NotFound);

		group.MapPost("/", CreateCategory)
			.WithName("CreateCategory")
			.WithDescription("Creates a new category")
			.RequireAuthorization("AdminPolicy")
			.Produces<CategoryDto>(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status409Conflict);

		group.MapPut("/{id}", UpdateCategory)
			.WithName("UpdateCategory")
			.WithDescription("Updates an existing category")
			.RequireAuthorization("AdminPolicy")
			.Produces<CategoryDto>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status404NotFound)
			.Produces(StatusCodes.Status409Conflict);

		group.MapDelete("/{id}", ArchiveCategory)
			.WithName("ArchiveCategory")
			.WithDescription("Archives a category (soft delete)")
			.RequireAuthorization("AdminPolicy")
			.Produces<CategoryDto>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status404NotFound);

		return app;
	}

	/// <summary>
	///   Gets all categories.
	/// </summary>
	private static async Task<IResult> GetAllCategories(
		ICategoryService categoryService,
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var result = await categoryService.GetCategoriesAsync(includeArchived, cancellationToken);

		return result.Success
			? Results.Ok(result.Value)
			: Results.Problem(result.Error ?? "Failed to retrieve categories");
	}

	/// <summary>
	///   Gets a category by ID.
	/// </summary>
	private static async Task<IResult> GetCategoryById(
		string id,
		ICategoryService categoryService,
		CancellationToken cancellationToken = default)
	{
		var result = await categoryService.GetCategoryByIdAsync(id, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.NotFound
				? Results.NotFound(new { error = result.Error })
				: Results.Problem(result.Error ?? "Failed to retrieve category");
		}

		return Results.Ok(result.Value);
	}

	/// <summary>
	///   Creates a new category.
	/// </summary>
	private static async Task<IResult> CreateCategory(
		CreateCategoryRequest request,
		ICategoryService categoryService,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(request.CategoryName))
		{
			return Results.BadRequest(new { error = "Category name is required" });
		}

		if (string.IsNullOrWhiteSpace(request.CategoryDescription))
		{
			return Results.BadRequest(new { error = "Category description is required" });
		}

		var result = await categoryService.CreateCategoryAsync(
			request.CategoryName,
			request.CategoryDescription,
			cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode switch
			{
				ResultErrorCode.Conflict => Results.Conflict(new { error = result.Error }),
				ResultErrorCode.Validation => Results.BadRequest(new { error = result.Error }),
				_ => Results.Problem(result.Error ?? "Failed to create category")
			};
		}

		return Results.Created($"/api/categories/{result.Value!.Id}", result.Value);
	}

	/// <summary>
	///   Updates an existing category.
	/// </summary>
	private static async Task<IResult> UpdateCategory(
		string id,
		UpdateCategoryRequest request,
		ICategoryService categoryService,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(request.CategoryName))
		{
			return Results.BadRequest(new { error = "Category name is required" });
		}

		if (string.IsNullOrWhiteSpace(request.CategoryDescription))
		{
			return Results.BadRequest(new { error = "Category description is required" });
		}

		var result = await categoryService.UpdateCategoryAsync(
			id,
			request.CategoryName,
			request.CategoryDescription,
			cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode switch
			{
				ResultErrorCode.NotFound => Results.NotFound(new { error = result.Error }),
				ResultErrorCode.Conflict => Results.Conflict(new { error = result.Error }),
				ResultErrorCode.Validation => Results.BadRequest(new { error = result.Error }),
				_ => Results.Problem(result.Error ?? "Failed to update category")
			};
		}

		return Results.Ok(result.Value);
	}

	/// <summary>
	///   Archives a category (soft delete).
	/// </summary>
	private static async Task<IResult> ArchiveCategory(
		string id,
		ICategoryService categoryService,
		HttpContext httpContext,
		CancellationToken cancellationToken = default)
	{
		var userId = httpContext.User.FindFirst("sub")?.Value ?? "unknown";
		var userName = httpContext.User.Identity?.Name ?? "Unknown User";
		var userEmail = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
		var archivedBy = new UserDto(userId, userName, userEmail);

		var result = await categoryService.ArchiveCategoryAsync(id, true, archivedBy, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.NotFound
				? Results.NotFound(new { error = result.Error })
				: Results.Problem(result.Error ?? "Failed to archive category");
		}

		return Results.Ok(result.Value);
	}
}

/// <summary>
///   Request model for creating a category.
/// </summary>
public record CreateCategoryRequest(string CategoryName, string CategoryDescription);

/// <summary>
///   Request model for updating a category.
/// </summary>
public record UpdateCategoryRequest(string CategoryName, string CategoryDescription);
