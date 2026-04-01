// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RestoreIssueCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to restore (unarchive) a previously archived issue.
/// </summary>
public record RestoreIssueCommand(string Id) : IRequest<Result<bool>>;

/// <summary>
///   Handler for restoring (unarchiving) an issue.
/// </summary>
public sealed class RestoreIssueCommandHandler : IRequestHandler<RestoreIssueCommand, Result<bool>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<RestoreIssueCommandHandler> _logger;

	public RestoreIssueCommandHandler(
		IRepository<Issue> repository,
		ILogger<RestoreIssueCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<bool>> Handle(RestoreIssueCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Restoring archived issue with ID: {IssueId}", request.Id);

		var existingResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.Id);
			return Result.Fail<bool>("Issue not found", ResultErrorCode.NotFound);
		}

		var issue = existingResult.Value;

		if (!issue.Archived)
		{
			_logger.LogWarning("Issue {IssueId} is not archived; restore skipped", request.Id);
			return Result.Fail<bool>("Issue is not archived", ResultErrorCode.Validation);
		}

		issue.Archived = false;
		issue.ArchivedBy = UserInfo.Empty;
		issue.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to restore issue: {Error}", result.Error);
			return Result.Fail<bool>(result.Error ?? "Failed to restore issue", result.ErrorCode);
		}

		_logger.LogInformation("Successfully restored issue with ID: {IssueId}", request.Id);
		return Result.Ok(true);
	}
}
