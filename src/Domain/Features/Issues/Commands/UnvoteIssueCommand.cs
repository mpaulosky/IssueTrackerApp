// Copyright (c) 2026. All rights reserved.

using Domain.Abstractions;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to remove a vote from an issue.
/// </summary>
public record UnvoteIssueCommand(
	string IssueId,
	string UserId) : IRequest<Result<IssueDto>>;

/// <summary>
///   Handler for removing a vote from an issue.
/// </summary>
public sealed class UnvoteIssueCommandHandler : IRequestHandler<UnvoteIssueCommand, Result<IssueDto>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<UnvoteIssueCommandHandler> _logger;

	public UnvoteIssueCommandHandler(
		IRepository<Issue> repository,
		ILogger<UnvoteIssueCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IssueDto>> Handle(UnvoteIssueCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("User {UserId} removing vote from issue {IssueId}", request.UserId, request.IssueId);

		var existingResult = await _repository.GetByIdAsync(request.IssueId, cancellationToken);

		if (existingResult.Failure)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.IssueId);
			return Result.Fail<IssueDto>(existingResult.Error ?? "Issue not found", existingResult.ErrorCode);
		}

		if (existingResult.Value is null)
		{
			return Result.Fail<IssueDto>("Issue not found", ResultErrorCode.NotFound);
		}

		var issue = existingResult.Value;
		issue.VotedBy ??= [];

		if (!issue.VotedBy.Contains(request.UserId))
		{
			_logger.LogWarning("User {UserId} has not voted on issue {IssueId}", request.UserId, request.IssueId);
			return Result.Fail<IssueDto>("Not voted", ResultErrorCode.Validation);
		}

		issue.VotedBy.Remove(request.UserId);
		issue.Votes = issue.VotedBy.Count;
		issue.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to remove vote from issue {IssueId}: {Error}", request.IssueId, result.Error);
			return Result.Fail<IssueDto>(result.Error ?? "Failed to remove vote", result.ErrorCode);
		}

		_logger.LogInformation("User {UserId} successfully removed vote from issue {IssueId}", request.UserId, request.IssueId);
		return Result.Ok(new IssueDto(result.Value!));
	}
}
