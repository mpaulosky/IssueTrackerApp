// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesByStatusQuery.cs
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
/// Query to get issue counts grouped by status.
/// </summary>
public record GetIssuesByStatusQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<Result<IReadOnlyList<IssuesByStatusDto>>>;

/// <summary>
/// Handler for GetIssuesByStatusQuery.
/// </summary>
public sealed class GetIssuesByStatusQueryHandler
	: IRequestHandler<GetIssuesByStatusQuery, Result<IReadOnlyList<IssuesByStatusDto>>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetIssuesByStatusQueryHandler> _logger;

	public GetIssuesByStatusQueryHandler(
		IRepository<Issue> repository,
		ILogger<GetIssuesByStatusQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<IssuesByStatusDto>>> Handle(
		GetIssuesByStatusQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Getting issues by status from {StartDate} to {EndDate}",
				request.StartDate, request.EndDate);

			var startDate = request.StartDate ?? DateTime.MinValue;
			var endDate = request.EndDate ?? DateTime.MaxValue;

			var result = await _repository.FindAsync(
				i => i.DateCreated >= startDate && i.DateCreated <= endDate,
				cancellationToken);

			if (result.Failure || result.Value is null)
			{
				_logger.LogWarning("Failed to retrieve issues for status analysis");
				return Result.Fail<IReadOnlyList<IssuesByStatusDto>>(
					result.Error ?? "Failed to retrieve issues");
			}

			var statusCounts = result.Value
				.GroupBy(i => i.Status.StatusName)
				.Select(g => new IssuesByStatusDto(g.Key, g.Count()))
				.OrderByDescending(x => x.Count)
				.ToList();

			_logger.LogInformation("Successfully retrieved {Count} status groups", statusCounts.Count);
			return Result.Ok<IReadOnlyList<IssuesByStatusDto>>(statusCounts);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting issues by status");
			return Result.Fail<IReadOnlyList<IssuesByStatusDto>>(
				$"Failed to get issues by status: {ex.Message}");
		}
	}
}
