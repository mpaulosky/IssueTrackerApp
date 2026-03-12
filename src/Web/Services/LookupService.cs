// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LookupService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
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
///   Implementation of ILookupService using repositories.
/// </summary>
public sealed class LookupService : ILookupService
{
	private readonly IRepository<Category> _categoryRepository;
	private readonly IRepository<Status> _statusRepository;

	public LookupService(
		IRepository<Category> categoryRepository,
		IRepository<Status> statusRepository)
	{
		_categoryRepository = categoryRepository;
		_statusRepository = statusRepository;
	}

	public async Task<Result<IEnumerable<CategoryDto>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
	{
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
			?? Enumerable.Empty<CategoryDto>();

		return Result.Ok(categories);
	}

	public async Task<Result<IEnumerable<StatusDto>>> GetStatusesAsync(CancellationToken cancellationToken = default)
	{
		var result = await _statusRepository.FindAsync(
			s => !s.Archived,
			cancellationToken);

		if (result.Failure)
		{
			return Result.Fail<IEnumerable<StatusDto>>(result.Error ?? "Failed to retrieve statuses");
		}

		var statuses = result.Value?
			.Select(s => new StatusDto(s))
			?? Enumerable.Empty<StatusDto>();

		return Result.Ok(statuses);
	}
}
