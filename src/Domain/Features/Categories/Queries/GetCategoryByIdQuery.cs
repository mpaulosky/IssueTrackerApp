// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetCategoryByIdQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Categories.Queries;

/// <summary>
///   Query to get a single category by ID.
/// </summary>
public record GetCategoryByIdQuery(string Id) : IRequest<Result<CategoryDto>>;

/// <summary>
///   Handler for getting a single category by ID.
/// </summary>
public sealed class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

	public GetCategoryByIdQueryHandler(
		IRepository<Category> repository,
		ILogger<GetCategoryByIdQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching category with ID: {CategoryId}", request.Id);

		var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (result.Failure || result.Value is null)
		{
			_logger.LogWarning("Category not found with ID: {CategoryId}", request.Id);
			return Result.Fail<CategoryDto>("Category not found", ResultErrorCode.NotFound);
		}

		_logger.LogInformation("Successfully fetched category with ID: {CategoryId}", request.Id);
		return Result.Ok(new CategoryDto(result.Value));
	}
}
