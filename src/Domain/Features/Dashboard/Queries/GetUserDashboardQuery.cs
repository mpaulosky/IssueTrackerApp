// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetUserDashboardQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Dashboard.Queries;

/// <summary>
///   Query to get dashboard data for a specific user.
/// </summary>
public record GetUserDashboardQuery(string UserId) : IRequest<Result<UserDashboardDto>>;

/// <summary>
///   Handler for getting user dashboard data.
/// </summary>
public sealed class GetUserDashboardQueryHandler : IRequestHandler<GetUserDashboardQuery, Result<UserDashboardDto>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetUserDashboardQueryHandler> _logger;

	public GetUserDashboardQueryHandler(
		IRepository<Issue> repository,
		ILogger<GetUserDashboardQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<UserDashboardDto>> Handle(
		GetUserDashboardQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching dashboard data for user: {UserId}", request.UserId);

		var allResult = await _repository.GetAllAsync(cancellationToken);

		if (allResult.Failure)
		{
			_logger.LogError("Failed to fetch issues for dashboard: {Error}", allResult.Error);
			return Result.Fail<UserDashboardDto>(
				allResult.Error ?? "Failed to fetch issues",
				allResult.ErrorCode);
		}

		var allIssues = allResult.Value?.ToList() ?? [];

		// Filter issues for the current user (non-archived)
		var userIssues = allIssues
			.Where(i => !i.Archived && i.Author.Id == request.UserId)
			.ToList();

		// Calculate stats
		var totalIssues = userIssues.Count;

		var openIssues = userIssues
			.Count(i => i.Status.StatusName.Equals("Open", StringComparison.OrdinalIgnoreCase) ||
			            i.Status.StatusName.Equals("In Progress", StringComparison.OrdinalIgnoreCase));

		var resolvedIssues = userIssues
			.Count(i => i.Status.StatusName.Equals("Resolved", StringComparison.OrdinalIgnoreCase) ||
			            i.Status.StatusName.Equals("Closed", StringComparison.OrdinalIgnoreCase));

		var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
		var thisWeekIssues = userIssues
			.Count(i => i.DateCreated >= oneWeekAgo);

		// Get recent issues (last 10)
		var recentIssues = userIssues
			.OrderByDescending(i => i.DateCreated)
			.Take(10)
			.Select(i => new IssueDto(i))
			.ToList();

		var dashboard = new UserDashboardDto(
			totalIssues,
			openIssues,
			resolvedIssues,
			thisWeekIssues,
			recentIssues);

		_logger.LogInformation(
			"Successfully fetched dashboard for user {UserId}: Total={Total}, Open={Open}, Resolved={Resolved}",
			request.UserId,
			totalIssues,
			openIssues,
			resolvedIssues);

		return Result.Ok(dashboard);
	}
}
