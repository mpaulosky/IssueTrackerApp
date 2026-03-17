// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Categories.Commands;
using Domain.Features.Categories.Queries;

using MediatR;

namespace Web.Services;

/// <summary>
///   Service facade for Category CRUD operations, wrapping MediatR calls.
/// </summary>
public interface ICategoryService
{
	/// <summary>
	///   Gets all categories with optional filtering.
	/// </summary>
	Task<Result<IEnumerable<CategoryDto>>> GetCategoriesAsync(
		bool includeArchived = false,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Gets a single category by ID.
	/// </summary>
	Task<Result<CategoryDto>> GetCategoryByIdAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	///   Creates a new category.
	/// </summary>
	Task<Result<CategoryDto>> CreateCategoryAsync(
		string categoryName,
		string categoryDescription,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Updates an existing category.
	/// </summary>
	Task<Result<CategoryDto>> UpdateCategoryAsync(
		string id,
		string categoryName,
		string categoryDescription,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Archives or unarchives a category.
	/// </summary>
	Task<Result<CategoryDto>> ArchiveCategoryAsync(
		string id,
		bool archive,
		UserDto archivedBy,
		CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of ICategoryService using MediatR.
/// </summary>
public sealed class CategoryService : ICategoryService
{
	private readonly IMediator _mediator;

	public CategoryService(IMediator mediator)
	{
		_mediator = mediator;
	}

	public async Task<Result<IEnumerable<CategoryDto>>> GetCategoriesAsync(
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var query = new GetCategoriesQuery(includeArchived);
		return await _mediator.Send(query, cancellationToken);
	}

	public async Task<Result<CategoryDto>> GetCategoryByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		var query = new GetCategoryByIdQuery(id);
		return await _mediator.Send(query, cancellationToken);
	}

	public async Task<Result<CategoryDto>> CreateCategoryAsync(
		string categoryName,
		string categoryDescription,
		CancellationToken cancellationToken = default)
	{
		var command = new CreateCategoryCommand(categoryName, categoryDescription);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<CategoryDto>> UpdateCategoryAsync(
		string id,
		string categoryName,
		string categoryDescription,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateCategoryCommand(id, categoryName, categoryDescription);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<CategoryDto>> ArchiveCategoryAsync(
		string id,
		bool archive,
		UserDto archivedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new ArchiveCategoryCommand(id, archive, archivedBy);
		return await _mediator.Send(command, cancellationToken);
	}
}
