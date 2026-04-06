// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LookupService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Models;

namespace Web.Services;

/// <summary>
///   Service facade for lookup operations (categories, statuses).
/// </summary>
public interface ILookupService
{
	/// <summary>
	///   Gets all available categories.
	/// </summary>
	Task<Result<IEnumerable<CategoryDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///   Gets all available statuses.
	/// </summary>
	Task<Result<IEnumerable<StatusDto>>> GetStatusesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of ILookupService using repositories with cache-aside reads
///   (30-minute TTL).  No write invalidation required — this service is read-only.
/// </summary>
public sealed class LookupService : ILookupService
{
	private const string CacheKeyCategories = "lookup_categories";
	private const string CacheKeyStatuses = "lookup_statuses";
	private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

	private readonly IRepository<Category> _categoryRepository;
	private readonly IRepository<Status> _statusRepository;
	private readonly DistributedCacheHelper _cacheHelper;

	public LookupService(
		IRepository<Category> categoryRepository,
		IRepository<Status> statusRepository,
		DistributedCacheHelper cacheHelper)
	{
		_categoryRepository = categoryRepository;
		_statusRepository = statusRepository;
		_cacheHelper = cacheHelper;
	}

	public async Task<Result<IEnumerable<CategoryDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
	{
		var cached = await _cacheHelper.GetAsync<List<CategoryDto>>(CacheKeyCategories, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok<IEnumerable<CategoryDto>>(cached);
		}

		var result = await _categoryRepository.FindAsync(
			c => !c.Archived,
			cancellationToken);

		if (result.Failure)
		{
			return Result.Fail<IEnumerable<CategoryDto>>(result.Error ?? "Failed to retrieve categories");
		}

		var categories = result.Value?
			.OrderBy(c => c.CategoryName)
			.Select(c => new CategoryDto(c))
			.ToList()
			?? [];

		await _cacheHelper.SetAsync(CacheKeyCategories, categories, CacheTtl, cancellationToken);

		return Result.Ok<IEnumerable<CategoryDto>>(categories);
	}

	public async Task<Result<IEnumerable<StatusDto>>> GetStatusesAsync(CancellationToken cancellationToken = default)
	{
		var cached = await _cacheHelper.GetAsync<List<StatusDto>>(CacheKeyStatuses, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok<IEnumerable<StatusDto>>(cached);
		}

		var result = await _statusRepository.FindAsync(
			s => !s.Archived,
			cancellationToken);

		if (result.Failure)
		{
			return Result.Fail<IEnumerable<StatusDto>>(result.Error ?? "Failed to retrieve statuses");
		}

		var statuses = result.Value?
			.Select(s => new StatusDto(s))
			.ToList()
			?? [];

		await _cacheHelper.SetAsync(CacheKeyStatuses, statuses, CacheTtl, cancellationToken);

		return Result.Ok<IEnumerable<StatusDto>>(statuses);
	}
}
