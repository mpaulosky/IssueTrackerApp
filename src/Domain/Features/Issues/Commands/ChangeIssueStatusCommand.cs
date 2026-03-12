// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ChangeIssueStatusCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Events;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to change the status of an issue.
/// </summary>
public record ChangeIssueStatusCommand(
	string Id,
	StatusDto NewStatus) : IRequest<Result<IssueDto>>;

/// <summary>
///   Handler for changing the status of an issue.
/// </summary>
public sealed class ChangeIssueStatusCommandHandler : IRequestHandler<ChangeIssueStatusCommand, Result<IssueDto>>
{
	private readonly IRepository<Issue> _repository;
	private readonly IMediator _mediator;
	private readonly ILogger<ChangeIssueStatusCommandHandler> _logger;

	public ChangeIssueStatusCommandHandler(
		IRepository<Issue> repository,
		IMediator mediator,
		ILogger<ChangeIssueStatusCommandHandler> logger)
	{
		_repository = repository;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<Result<IssueDto>> Handle(ChangeIssueStatusCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation(
			"Changing status of issue {IssueId} to {NewStatus}",
			request.Id,
			request.NewStatus.StatusName);

		var existingResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.Id);
			return Result.Fail<IssueDto>("Issue not found", ResultErrorCode.NotFound);
		}

		var issue = existingResult.Value;
		var oldStatus = issue.Status.StatusName;
		issue.Status = request.NewStatus;
		issue.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to change issue status: {Error}", result.Error);
			return Result.Fail<IssueDto>(result.Error ?? "Failed to change issue status", result.ErrorCode);
		}

		_logger.LogInformation(
			"Successfully changed status of issue {IssueId} to {NewStatus}",
			request.Id,
			request.NewStatus.StatusName);

		// Publish status changed event for email notifications
		await _mediator.Publish(new IssueStatusChangedEvent
		{
			IssueId = issue.Id,
			IssueTitle = issue.Title,
			OldStatus = oldStatus,
			NewStatus = request.NewStatus.StatusName,
			IssueOwner = issue.Author.Id
		}, cancellationToken);

		return Result.Ok(new IssueDto(result.Value!));
	}
}
