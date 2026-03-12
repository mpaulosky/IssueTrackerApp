// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetTopContributorsQuery.cs
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
/// Query to get top contributors based on closed issues and comment counts.
/// </summary>
public record GetTopContributorsQuery(DateTime? StartDate, DateTime? EndDate, int TopCount = 10) : IRequest<Result<IReadOnlyList<TopContributorDto>>>;

/// <summary>
/// Handler for GetTopContributorsQuery.
/// </summary>
public sealed class GetTopContributorsQueryHandler
	: IRequestHandler<GetTopContributorsQuery, Result<IReadOnlyList<TopContributorDto>>>
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly IRepository<Comment> _commentRepository;
	private readonly ILogger<GetTopContributorsQueryHandler> _logger;

	public GetTopContributorsQueryHandler(
		IRepository<Issue> issueRepository,
		IRepository<Comment> commentRepository,
		ILogger<GetTopContributorsQueryHandler> logger)
	{
		_issueRepository = issueRepository;
		_commentRepository = commentRepository;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<TopContributorDto>>> Handle(
		GetTopContributorsQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Getting top {TopCount} contributors from {StartDate} to {EndDate}",
				request.TopCount, request.StartDate, request.EndDate);

			var startDate = request.StartDate ?? DateTime.MinValue;
			var endDate = request.EndDate ?? DateTime.MaxValue;

			// Get closed issues
			var issuesResult = await _issueRepository.FindAsync(
				i => i.DateModified.HasValue &&
					i.DateModified >= startDate &&
					i.DateModified <= endDate &&
					(i.Status.StatusName.Equals("Closed", StringComparison.OrdinalIgnoreCase) || i.Archived),
				cancellationToken);

			// Get comments
			var commentsResult = await _commentRepository.FindAsync(
				c => c.DateCreated >= startDate && c.DateCreated <= endDate,
				cancellationToken);

			if (issuesResult.Failure && commentsResult.Failure)
			{
				_logger.LogWarning("Failed to retrieve data for contributor analysis");
				return Result.Fail<IReadOnlyList<TopContributorDto>>(
					"Failed to retrieve issues and comments");
			}

			var issues = issuesResult.Value?.ToList() ?? [];
			var comments = commentsResult.Value?.ToList() ?? [];

			// Count closed issues by author
			var issueClosers = issues
				.GroupBy(i => new { i.Author.Id, i.Author.Name })
				.ToDictionary(g => g.Key.Id, g => new { g.Key.Name, Count = g.Count() });

			// Count comments by author
			var commentAuthors = comments
				.GroupBy(c => c.Author.Id)
				.ToDictionary(g => g.Key, g => g.Count());

			// Combine data
			var allContributorIds = issueClosers.Keys.Union(commentAuthors.Keys).ToList();

			var contributors = allContributorIds
				.Select(userId =>
				{
					var issuesClosed = issueClosers.GetValueOrDefault(userId)?.Count ?? 0;
					var commentsCount = commentAuthors.GetValueOrDefault(userId, 0);
					var userName = issueClosers.GetValueOrDefault(userId)?.Name ??
						comments.FirstOrDefault(c => c.Author.Id == userId)?.Author.Name ?? "Unknown";

					return new TopContributorDto(userId, userName, issuesClosed, commentsCount);
				})
				.OrderByDescending(c => c.IssuesClosed + c.CommentsCount)
				.Take(request.TopCount)
				.ToList();

			_logger.LogInformation("Successfully retrieved top {Count} contributors", contributors.Count);
			return Result.Ok<IReadOnlyList<TopContributorDto>>(contributors);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting top contributors");
			return Result.Fail<IReadOnlyList<TopContributorDto>>(
				$"Failed to get top contributors: {ex.Message}");
		}
	}
}
