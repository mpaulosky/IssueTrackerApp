// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssueCommentsQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Comments.Queries;

/// <summary>
///   Query to get all comments for a specific issue.
/// </summary>
public record GetIssueCommentsQuery(
	string IssueId,
	bool IncludeArchived = false) : IRequest<Result<IReadOnlyList<CommentDto>>>;

/// <summary>
///   Handler for getting all comments for a specific issue.
/// </summary>
public sealed class GetIssueCommentsQueryHandler : IRequestHandler<GetIssueCommentsQuery, Result<IReadOnlyList<CommentDto>>>
{
	private readonly IRepository<Comment> _repository;
	private readonly ILogger<GetIssueCommentsQueryHandler> _logger;

	public GetIssueCommentsQueryHandler(
		IRepository<Comment> repository,
		ILogger<GetIssueCommentsQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<CommentDto>>> Handle(GetIssueCommentsQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching comments for issue: {IssueId}", request.IssueId);

		var issueObjectId = ObjectId.Parse(request.IssueId);

		var result = await _repository.FindAsync(
			c => c.Issue.Id == issueObjectId && (request.IncludeArchived || !c.Archived),
			cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to fetch comments: {Error}", result.Error);
			return Result.Fail<IReadOnlyList<CommentDto>>(result.Error ?? "Failed to fetch comments", result.ErrorCode);
		}

		var comments = result.Value?
			.OrderByDescending(c => c.DateCreated)
			.Select(c => new CommentDto(c))
			.ToList() ?? [];

		_logger.LogInformation("Successfully fetched {Count} comments for issue: {IssueId}", comments.Count, request.IssueId);
		return Result.Ok<IReadOnlyList<CommentDto>>(comments);
	}
}
