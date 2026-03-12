// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SearchIssuesQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Issues.Queries;

/// <summary>
///   Query to search issues with text search, filters, and pagination.
/// </summary>
public record SearchIssuesQuery(IssueSearchRequest Request) : IRequest<Result<PagedResult<IssueDto>>>;

/// <summary>
///   Handler for searching issues with full-text search and filters.
/// </summary>
public sealed class SearchIssuesQueryHandler : IRequestHandler<SearchIssuesQuery, Result<PagedResult<IssueDto>>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<SearchIssuesQueryHandler> _logger;

	public SearchIssuesQueryHandler(
		IRepository<Issue> repository,
		ILogger<SearchIssuesQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<PagedResult<IssueDto>>> Handle(
		SearchIssuesQuery query,
		CancellationToken cancellationToken)
	{
		var request = query.Request;

		_logger.LogInformation(
			"Searching issues - SearchText: {SearchText}, Status: {Status}, Category: {Category}, " +
			"Author: {Author}, DateFrom: {DateFrom}, DateTo: {DateTo}, Page: {Page}, PageSize: {PageSize}",
			request.SearchText ?? "None",
			request.StatusFilter ?? "All",
			request.CategoryFilter ?? "All",
			request.AuthorId ?? "All",
			request.DateFrom?.ToString() ?? "None",
			request.DateTo?.ToString() ?? "None",
			request.Page,
			request.PageSize);

		var allResult = await _repository.GetAllAsync(cancellationToken);

		if (allResult.Failure)
		{
			_logger.LogError("Failed to fetch issues for search: {Error}", allResult.Error);
			return Result.Fail<PagedResult<IssueDto>>(
				allResult.Error ?? "Failed to fetch issues",
				allResult.ErrorCode);
		}

		var issues = allResult.Value?.ToList() ?? [];

		// Apply archive filter
		if (!request.IncludeArchived)
		{
			issues = issues.Where(i => !i.Archived).ToList();
		}

		// Apply text search filter (searches title and description)
		if (!string.IsNullOrWhiteSpace(request.SearchText))
		{
			var searchText = request.SearchText.Trim();
			issues = issues
				.Where(i =>
					i.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
					i.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		// Apply status filter
		if (!string.IsNullOrWhiteSpace(request.StatusFilter))
		{
			issues = issues
				.Where(i => i.Status.StatusName.Equals(request.StatusFilter, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		// Apply category filter
		if (!string.IsNullOrWhiteSpace(request.CategoryFilter))
		{
			issues = issues
				.Where(i => i.Category.CategoryName.Equals(request.CategoryFilter, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		// Apply author filter
		if (!string.IsNullOrWhiteSpace(request.AuthorId))
		{
			issues = issues
				.Where(i => i.Author.Id.Equals(request.AuthorId, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		// Apply date range filter
		if (request.DateFrom.HasValue)
		{
			var fromDate = request.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
			issues = issues.Where(i => i.DateCreated >= fromDate).ToList();
		}

		if (request.DateTo.HasValue)
		{
			var toDate = request.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
			issues = issues.Where(i => i.DateCreated <= toDate).ToList();
		}

		var totalCount = issues.Count;

		// Apply pagination
		var pagedIssues = issues
			.OrderByDescending(i => i.DateCreated)
			.Skip((request.Page - 1) * request.PageSize)
			.Take(request.PageSize)
			.Select(i => new IssueDto(i))
			.ToList();

		var result = PagedResult<IssueDto>.Create(
			pagedIssues,
			totalCount,
			request.Page,
			request.PageSize);

		_logger.LogInformation(
			"Search completed - Found {Count} issues out of {Total} (Page {Page} of {TotalPages})",
			pagedIssues.Count,
			totalCount,
			request.Page,
			result.TotalPages);

		return Result.Ok(result);
	}
}
