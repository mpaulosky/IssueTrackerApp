// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteIssueCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to soft delete (archive) an issue.
/// </summary>
public record DeleteIssueCommand(
	string Id,
	UserDto ArchivedBy) : IRequest<Result<bool>>;

/// <summary>
///   Handler for soft deleting (archiving) an issue.
/// </summary>
public sealed class DeleteIssueCommandHandler : IRequestHandler<DeleteIssueCommand, Result<bool>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<DeleteIssueCommandHandler> _logger;

	public DeleteIssueCommandHandler(
		IRepository<Issue> repository,
		ILogger<DeleteIssueCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<bool>> Handle(DeleteIssueCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Archiving issue with ID: {IssueId}", request.Id);

		var existingResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.Id);
			return Result.Fail<bool>("Issue not found", ResultErrorCode.NotFound);
		}

		var issue = existingResult.Value;
		issue.Archived = true;
		issue.ArchivedBy = request.ArchivedBy;
		issue.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to archive issue: {Error}", result.Error);
			return Result.Fail<bool>(result.Error ?? "Failed to archive issue", result.ErrorCode);
		}

		_logger.LogInformation("Successfully archived issue with ID: {IssueId}", request.Id);
		return Result.Ok(true);
	}
}
