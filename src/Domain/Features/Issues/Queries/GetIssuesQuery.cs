// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Issues.Queries;

/// <summary>
///   Query to get a paginated list of issues with optional filters.
/// </summary>
public record GetIssuesQuery(
	int Page = 1,
	int PageSize = 10,
	string? StatusFilter = null,
	string? CategoryFilter = null,
	bool IncludeArchived = false) : IRequest<Result<PaginatedResponse<IssueDto>>>;

/// <summary>
///   Handler for getting a paginated list of issues.
/// </summary>
public sealed class GetIssuesQueryHandler : IRequestHandler<GetIssuesQuery, Result<PaginatedResponse<IssueDto>>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetIssuesQueryHandler> _logger;

	public GetIssuesQueryHandler(
		IRepository<Issue> repository,
		ILogger<GetIssuesQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<PaginatedResponse<IssueDto>>> Handle(
		GetIssuesQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation(
			"Fetching issues - Page: {Page}, PageSize: {PageSize}, Status: {Status}, Category: {Category}",
			request.Page,
			request.PageSize,
			request.StatusFilter ?? "All",
			request.CategoryFilter ?? "All");

		var allResult = await _repository.GetAllAsync(cancellationToken);

		if (allResult.Failure)
		{
			_logger.LogError("Failed to fetch issues: {Error}", allResult.Error);
			return Result.Fail<PaginatedResponse<IssueDto>>(
				allResult.Error ?? "Failed to fetch issues",
				allResult.ErrorCode);
		}

		var issues = allResult.Value?.ToList() ?? [];

		// Apply filters
		if (!request.IncludeArchived)
		{
			issues = issues.Where(i => !i.Archived).ToList();
		}

		if (!string.IsNullOrWhiteSpace(request.StatusFilter))
		{
			issues = issues
				.Where(i => i.Status.StatusName.Equals(request.StatusFilter, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		if (!string.IsNullOrWhiteSpace(request.CategoryFilter))
		{
			issues = issues
				.Where(i => i.Category.CategoryName.Equals(request.CategoryFilter, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		var total = issues.Count;

		// Apply pagination
		var pagedIssues = issues
			.OrderByDescending(i => i.DateCreated)
			.Skip((request.Page - 1) * request.PageSize)
			.Take(request.PageSize)
			.Select(i => new IssueDto(i))
			.ToList();

		var response = new PaginatedResponse<IssueDto>(
			pagedIssues,
			total,
			request.Page,
			request.PageSize);

		_logger.LogInformation(
			"Successfully fetched {Count} issues out of {Total}",
			pagedIssues.Count,
			total);

		return Result.Ok(response);
	}
}
