// Copyright (c) 2026. All rights reserved.

using Domain.Abstractions;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to cast a vote on an issue.
/// </summary>
public record VoteIssueCommand(
	string IssueId,
	string UserId) : IRequest<Result<IssueDto>>;

/// <summary>
///   Handler for casting a vote on an issue.
/// </summary>
public sealed class VoteIssueCommandHandler : IRequestHandler<VoteIssueCommand, Result<IssueDto>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<VoteIssueCommandHandler> _logger;

	public VoteIssueCommandHandler(
		IRepository<Issue> repository,
		ILogger<VoteIssueCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IssueDto>> Handle(VoteIssueCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("User {UserId} voting on issue {IssueId}", request.UserId, request.IssueId);

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

		if (issue.VotedBy.Contains(request.UserId))
		{
			_logger.LogWarning("User {UserId} has already voted on issue {IssueId}", request.UserId, request.IssueId);
			return Result.Fail<IssueDto>("Already voted", ResultErrorCode.Validation);
		}

		issue.VotedBy.Add(request.UserId);
		issue.Votes = issue.VotedBy.Count;
		issue.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to save vote on issue {IssueId}: {Error}", request.IssueId, result.Error);
			return Result.Fail<IssueDto>(result.Error ?? "Failed to save vote", result.ErrorCode);
		}

		_logger.LogInformation("User {UserId} successfully voted on issue {IssueId}", request.UserId, request.IssueId);
		return Result.Ok(new IssueDto(result.Value!));
	}
}
