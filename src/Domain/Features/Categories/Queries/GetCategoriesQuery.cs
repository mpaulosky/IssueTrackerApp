// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetCategoriesQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Categories.Queries;

/// <summary>
///   Query to get all categories with optional filtering.
/// </summary>
public record GetCategoriesQuery(bool IncludeArchived = false) : IRequest<Result<IEnumerable<CategoryDto>>>;

/// <summary>
///   Handler for getting all categories.
/// </summary>
public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<IEnumerable<CategoryDto>>>
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<GetCategoriesQueryHandler> _logger;

	public GetCategoriesQueryHandler(
		IRepository<Category> repository,
		ILogger<GetCategoriesQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IEnumerable<CategoryDto>>> Handle(
		GetCategoriesQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching categories - IncludeArchived: {IncludeArchived}", request.IncludeArchived);

		Result<IEnumerable<Category>> result;

		if (request.IncludeArchived)
		{
			result = await _repository.GetAllAsync(cancellationToken);
		}
		else
		{
			result = await _repository.FindAsync(c => !c.Archived, cancellationToken);
		}

		if (result.Failure)
		{
			_logger.LogError("Failed to fetch categories: {Error}", result.Error);
			return Result.Fail<IEnumerable<CategoryDto>>(
				result.Error ?? "Failed to fetch categories",
				result.ErrorCode);
		}

		var categories = result.Value?
			.OrderBy(c => c.CategoryName)
			.Select(c => new CategoryDto(c))
			?? Enumerable.Empty<CategoryDto>();

		_logger.LogInformation("Successfully fetched {Count} categories", categories.Count());
		return Result.Ok(categories);
	}
}
