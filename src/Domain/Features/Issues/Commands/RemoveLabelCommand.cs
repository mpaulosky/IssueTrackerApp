// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RemoveLabelCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Events;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to remove a label from an issue.
/// </summary>
public record RemoveLabelCommand(string IssueId, string Label) : IRequest<Result<IssueDto>>;

/// <summary>
///   Handler for removing a label from an issue.
/// </summary>
public sealed class RemoveLabelCommandHandler : IRequestHandler<RemoveLabelCommand, Result<IssueDto>>
{
	private readonly IRepository<Issue> _repository;
	private readonly IMediator _mediator;
	private readonly ILogger<RemoveLabelCommandHandler> _logger;

	public RemoveLabelCommandHandler(
		IRepository<Issue> repository,
		IMediator mediator,
		ILogger<RemoveLabelCommandHandler> logger)
	{
		_repository = repository;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<Result<IssueDto>> Handle(RemoveLabelCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Removing label '{Label}' from issue {IssueId}", request.Label, request.IssueId);

		var existingResult = await _repository.GetByIdAsync(request.IssueId, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.IssueId);
			return Result.Fail<IssueDto>("Issue not found", ResultErrorCode.NotFound);
		}

		var issue = existingResult.Value;
		var normalised = request.Label.Trim().ToLowerInvariant();
		issue.Labels ??= [];

		if (!issue.Labels.Contains(normalised))
		{
			_logger.LogInformation("Label '{Label}' not present on issue {IssueId} — no-op", normalised, request.IssueId);
			return Result.Ok(new IssueDto(issue));
		}

		issue.Labels.Remove(normalised);
		issue.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to remove label from issue {IssueId}: {Error}", request.IssueId, result.Error);
			return Result.Fail<IssueDto>(result.Error ?? "Failed to remove label", result.ErrorCode);
		}

		var dto = new IssueDto(result.Value!);

		await _mediator.Publish(new IssueUpdatedEvent { Issue = dto }, cancellationToken);

		_logger.LogInformation("Successfully removed label '{Label}' from issue {IssueId}", normalised, request.IssueId);
		return Result.Ok(dto);
	}
}
