// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesByCategoryQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.DTOs.Analytics;
using Domain.Models;

using Microsoft.Extensions.Logging;

namespace Domain.Features.Analytics.Queries;

/// <summary>
/// Query to get issue counts grouped by category.
/// </summary>
public record GetIssuesByCategoryQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<Result<IReadOnlyList<IssuesByCategoryDto>>>;

/// <summary>
/// Handler for GetIssuesByCategoryQuery.
/// </summary>
public sealed class GetIssuesByCategoryQueryHandler
	: IRequestHandler<GetIssuesByCategoryQuery, Result<IReadOnlyList<IssuesByCategoryDto>>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetIssuesByCategoryQueryHandler> _logger;

	public GetIssuesByCategoryQueryHandler(
		IRepository<Issue> repository,
		ILogger<GetIssuesByCategoryQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<IssuesByCategoryDto>>> Handle(
		GetIssuesByCategoryQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Getting issues by category from {StartDate} to {EndDate}",
				request.StartDate, request.EndDate);

			var startDate = request.StartDate ?? DateTime.MinValue;
			var endDate = request.EndDate ?? DateTime.MaxValue;

			var result = await _repository.FindAsync(
				i => i.DateCreated >= startDate && i.DateCreated <= endDate,
				cancellationToken);

			if (result.Failure || result.Value is null)
			{
				_logger.LogWarning("Failed to retrieve issues for category analysis");
				return Result.Fail<IReadOnlyList<IssuesByCategoryDto>>(
					result.Error ?? "Failed to retrieve issues");
			}

			var categoryCounts = result.Value
				.GroupBy(i => i.Category.CategoryName)
				.Select(g => new IssuesByCategoryDto(g.Key, g.Count()))
				.OrderByDescending(x => x.Count)
				.ToList();

			_logger.LogInformation("Successfully retrieved {Count} category groups", categoryCounts.Count);
			return Result.Ok<IReadOnlyList<IssuesByCategoryDto>>(categoryCounts);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting issues by category");
			return Result.Fail<IReadOnlyList<IssuesByCategoryDto>>(
				$"Failed to get issues by category: {ex.Message}");
		}
	}
}
