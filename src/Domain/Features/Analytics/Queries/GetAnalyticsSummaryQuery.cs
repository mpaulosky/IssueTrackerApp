// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetAnalyticsSummaryQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.DTOs.Analytics;

using Microsoft.Extensions.Logging;

namespace Domain.Features.Analytics.Queries;

/// <summary>
/// Query to get comprehensive analytics summary for dashboard.
/// </summary>
public record GetAnalyticsSummaryQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<Result<AnalyticsSummaryDto>>;

/// <summary>
/// Handler for GetAnalyticsSummaryQuery.
/// </summary>
public sealed class GetAnalyticsSummaryQueryHandler
	: IRequestHandler<GetAnalyticsSummaryQuery, Result<AnalyticsSummaryDto>>
{
	private readonly IMediator _mediator;
	private readonly ILogger<GetAnalyticsSummaryQueryHandler> _logger;

	public GetAnalyticsSummaryQueryHandler(
		IMediator mediator,
		ILogger<GetAnalyticsSummaryQueryHandler> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<Result<AnalyticsSummaryDto>> Handle(
		GetAnalyticsSummaryQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Getting analytics summary from {StartDate} to {EndDate}",
				request.StartDate, request.EndDate);

			// Execute all queries in parallel
			var statusTask = _mediator.Send(
				new GetIssuesByStatusQuery(request.StartDate, request.EndDate), cancellationToken);
			var categoryTask = _mediator.Send(
				new GetIssuesByCategoryQuery(request.StartDate, request.EndDate), cancellationToken);
			var overTimeTask = _mediator.Send(
				new GetIssuesOverTimeQuery(request.StartDate, request.EndDate), cancellationToken);
			var resolutionTask = _mediator.Send(
				new GetResolutionTimesQuery(request.StartDate, request.EndDate), cancellationToken);
			var contributorsTask = _mediator.Send(
				new GetTopContributorsQuery(request.StartDate, request.EndDate, 10), cancellationToken);

			await Task.WhenAll(statusTask, categoryTask, overTimeTask, resolutionTask, contributorsTask);

			var statusResult = await statusTask;
			var categoryResult = await categoryTask;
			var overTimeResult = await overTimeTask;
			var resolutionResult = await resolutionTask;
			var contributorsResult = await contributorsTask;

			// Check for failures
			if (statusResult.Failure || categoryResult.Failure || overTimeResult.Failure ||
				resolutionResult.Failure || contributorsResult.Failure)
			{
				_logger.LogWarning("One or more analytics queries failed");
				var errors = new[]
				{
					statusResult.Error,
					categoryResult.Error,
					overTimeResult.Error,
					resolutionResult.Error,
					contributorsResult.Error
				}.Where(e => !string.IsNullOrEmpty(e));

				return Result.Fail<AnalyticsSummaryDto>($"Analytics query failed: {string.Join(", ", errors)}");
			}

			// Calculate summary statistics
			var byStatus = statusResult.Value ?? [];
			var byCategory = categoryResult.Value ?? [];
			var overTime = overTimeResult.Value ?? [];
			var resolutionTimes = resolutionResult.Value ?? [];
			var topContributors = contributorsResult.Value ?? [];

			var totalIssues = byStatus.Sum(s => s.Count);
			var openIssues = byStatus
				.Where(s => !s.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
				.Sum(s => s.Count);
			var closedIssues = byStatus
				.Where(s => s.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
				.Sum(s => s.Count);
			var averageResolutionHours = resolutionTimes.Any()
				? resolutionTimes.Average(r => r.AverageHours)
				: 0;

			var summary = new AnalyticsSummaryDto(
				totalIssues,
				openIssues,
				closedIssues,
				averageResolutionHours,
				byStatus,
				byCategory,
				overTime,
				topContributors);

			_logger.LogInformation("Successfully retrieved analytics summary");
			return Result.Ok(summary);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting analytics summary");
			return Result.Fail<AnalyticsSummaryDto>($"Failed to get analytics summary: {ex.Message}");
		}
	}
}
