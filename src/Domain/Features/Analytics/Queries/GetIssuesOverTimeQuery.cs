// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesOverTimeQuery.cs
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
/// Query to get issue creation and closure counts over time.
/// </summary>
public record GetIssuesOverTimeQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<Result<IReadOnlyList<IssuesOverTimeDto>>>;

/// <summary>
/// Handler for GetIssuesOverTimeQuery.
/// </summary>
public sealed class GetIssuesOverTimeQueryHandler
	: IRequestHandler<GetIssuesOverTimeQuery, Result<IReadOnlyList<IssuesOverTimeDto>>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetIssuesOverTimeQueryHandler> _logger;

	public GetIssuesOverTimeQueryHandler(
		IRepository<Issue> repository,
		ILogger<GetIssuesOverTimeQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<IssuesOverTimeDto>>> Handle(
		GetIssuesOverTimeQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Getting issues over time from {StartDate} to {EndDate}",
				request.StartDate, request.EndDate);

			var startDate = request.StartDate ?? DateTime.MinValue;
			var endDate = request.EndDate ?? DateTime.MaxValue;

			var result = await _repository.FindAsync(
				i => i.DateCreated >= startDate && i.DateCreated <= endDate,
				cancellationToken);

			if (result.Failure || result.Value is null)
			{
				_logger.LogWarning("Failed to retrieve issues for time series analysis");
				return Result.Fail<IReadOnlyList<IssuesOverTimeDto>>(
					result.Error ?? "Failed to retrieve issues");
			}

			var issues = result.Value.ToList();

			// Group by date (day) for created issues
			var createdByDate = issues
				.GroupBy(i => i.DateCreated.Date)
				.ToDictionary(g => g.Key, g => g.Count());

			// Group by date (day) for closed issues (assuming Archived means closed)
			var closedByDate = issues
				.Where(i => i.DateModified.HasValue &&
					(i.Status.StatusName.Equals("Closed", StringComparison.OrdinalIgnoreCase) || i.Archived))
				.GroupBy(i => i.DateModified!.Value.Date)
				.ToDictionary(g => g.Key, g => g.Count());

			// Get all unique dates
			var allDates = createdByDate.Keys
				.Union(closedByDate.Keys)
				.OrderBy(d => d)
				.ToList();

			var overTime = allDates
				.Select(date => new IssuesOverTimeDto(
					date,
					createdByDate.GetValueOrDefault(date, 0),
					closedByDate.GetValueOrDefault(date, 0)))
				.ToList();

			_logger.LogInformation("Successfully retrieved {Count} time series data points", overTime.Count);
			return Result.Ok<IReadOnlyList<IssuesOverTimeDto>>(overTime);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting issues over time");
			return Result.Fail<IReadOnlyList<IssuesOverTimeDto>>(
				$"Failed to get issues over time: {ex.Message}");
		}
	}
}
