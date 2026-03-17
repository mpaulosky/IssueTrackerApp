// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetResolutionTimesQuery.cs
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
/// Query to get average resolution times grouped by category.
/// </summary>
public record GetResolutionTimesQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<Result<IReadOnlyList<ResolutionTimeDto>>>;

/// <summary>
/// Handler for GetResolutionTimesQuery.
/// </summary>
public sealed class GetResolutionTimesQueryHandler
	: IRequestHandler<GetResolutionTimesQuery, Result<IReadOnlyList<ResolutionTimeDto>>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetResolutionTimesQueryHandler> _logger;

	public GetResolutionTimesQueryHandler(
		IRepository<Issue> repository,
		ILogger<GetResolutionTimesQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<ResolutionTimeDto>>> Handle(
		GetResolutionTimesQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Getting resolution times from {StartDate} to {EndDate}",
				request.StartDate, request.EndDate);

			var startDate = request.StartDate ?? DateTime.MinValue;
			var endDate = request.EndDate ?? DateTime.MaxValue;

			var result = await _repository.FindAsync(
				i => i.DateCreated >= startDate &&
					i.DateCreated <= endDate &&
					i.DateModified.HasValue &&
					(i.Status.StatusName.Equals("Closed", StringComparison.OrdinalIgnoreCase) || i.Archived),
				cancellationToken);

			if (result.Failure || result.Value is null)
			{
				_logger.LogWarning("Failed to retrieve issues for resolution time analysis");
				return Result.Fail<IReadOnlyList<ResolutionTimeDto>>(
					result.Error ?? "Failed to retrieve issues");
			}

			var resolutionTimes = result.Value
				.Where(i => i.DateModified.HasValue)
				.GroupBy(i => i.Category.CategoryName)
				.Select(g => new ResolutionTimeDto(
					g.Key,
					g.Average(i => (i.DateModified!.Value - i.DateCreated).TotalHours)))
				.OrderBy(x => x.Category)
				.ToList();

			_logger.LogInformation("Successfully retrieved {Count} resolution time groups", resolutionTimes.Count);
			return Result.Ok<IReadOnlyList<ResolutionTimeDto>>(resolutionTimes);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting resolution times");
			return Result.Fail<IReadOnlyList<ResolutionTimeDto>>(
				$"Failed to get resolution times: {ex.Message}");
		}
	}
}
