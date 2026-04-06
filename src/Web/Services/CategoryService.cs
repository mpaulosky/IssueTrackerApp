// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
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
///   Implementation of ICategoryService using MediatR with cache-aside reads
///   (60-minute TTL) and write-through invalidation.
/// </summary>
public sealed class CategoryService : ICategoryService
{
	private const string CacheKeyList = "categories_list";
	private const string CacheKeyPrefix = "category_";
	private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

	private readonly IMediator _mediator;
	private readonly DistributedCacheHelper _cacheHelper;

	public CategoryService(IMediator mediator, DistributedCacheHelper cacheHelper)
	{
		_mediator = mediator;
		_cacheHelper = cacheHelper;
	}

	public async Task<Result<IEnumerable<CategoryDto>>> GetCategoriesAsync(
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{CacheKeyList}_{includeArchived}";

		var cached = await _cacheHelper.GetAsync<List<CategoryDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok<IEnumerable<CategoryDto>>(cached);
		}

		var query = new GetCategoriesQuery(includeArchived);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await _cacheHelper.SetAsync(cacheKey, result.Value.ToList(), CacheTtl, cancellationToken);
		}

		return result;
	}

	public async Task<Result<CategoryDto>> GetCategoryByIdAsync(
		string id,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{CacheKeyPrefix}{id}";

		var cached = await _cacheHelper.GetAsync<CategoryDto>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok(cached);
		}

		var query = new GetCategoryByIdQuery(id);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await _cacheHelper.SetAsync(cacheKey, result.Value, CacheTtl, cancellationToken);
		}

		return result;
	}

	public async Task<Result<CategoryDto>> CreateCategoryAsync(
		string categoryName,
		string categoryDescription,
		CancellationToken cancellationToken = default)
	{
		var command = new CreateCategoryCommand(categoryName, categoryDescription);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await InvalidateListCacheAsync(cancellationToken);
		}

		return result;
	}

	public async Task<Result<CategoryDto>> UpdateCategoryAsync(
		string id,
		string categoryName,
		string categoryDescription,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateCategoryCommand(id, categoryName, categoryDescription);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cacheHelper.RemoveAsync($"{CacheKeyPrefix}{id}", cancellationToken);
			await InvalidateListCacheAsync(cancellationToken);
		}

		return result;
	}

	public async Task<Result<CategoryDto>> ArchiveCategoryAsync(
		string id,
		bool archive,
		UserDto archivedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new ArchiveCategoryCommand(id, archive, archivedBy);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cacheHelper.RemoveAsync($"{CacheKeyPrefix}{id}", cancellationToken);
			await InvalidateListCacheAsync(cancellationToken);
		}

		return result;
	}

	private async Task InvalidateListCacheAsync(CancellationToken ct)
	{
		await _cacheHelper.RemoveAsync($"{CacheKeyList}_True", ct);
		await _cacheHelper.RemoveAsync($"{CacheKeyList}_False", ct);
	}
}
