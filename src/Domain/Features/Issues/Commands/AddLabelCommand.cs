// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddLabelCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Events;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to add a label to an issue.
/// </summary>
public record AddLabelCommand(string IssueId, string Label) : IRequest<Result<IssueDto>>;

/// <summary>
///   Handler for adding a label to an issue.
/// </summary>
public sealed class AddLabelCommandHandler : IRequestHandler<AddLabelCommand, Result<IssueDto>>
{
	private const int MaxLabels = 10;

	private readonly IRepository<Issue> _repository;
	private readonly IMediator _mediator;
	private readonly ILogger<AddLabelCommandHandler> _logger;

	public AddLabelCommandHandler(
		IRepository<Issue> repository,
		IMediator mediator,
		ILogger<AddLabelCommandHandler> logger)
	{
		_repository = repository;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<Result<IssueDto>> Handle(AddLabelCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Adding label '{Label}' to issue {IssueId}", request.Label, request.IssueId);

		var existingResult = await _repository.GetByIdAsync(request.IssueId, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.IssueId);
			return Result.Fail<IssueDto>("Issue not found", ResultErrorCode.NotFound);
		}

		var issue = existingResult.Value;
		var normalised = request.Label.Trim().ToLowerInvariant();
		issue.Labels ??= [];

		if (issue.Labels.Contains(normalised))
		{
			_logger.LogInformation("Label '{Label}' already present on issue {IssueId} — no-op", normalised, request.IssueId);
			return Result.Ok(new IssueDto(issue));
		}

		if (issue.Labels.Count >= MaxLabels)
		{
			_logger.LogWarning("Issue {IssueId} already has {Count} labels (max {Max})", request.IssueId, issue.Labels.Count, MaxLabels);
			return Result.Fail<IssueDto>($"An issue may not have more than {MaxLabels} labels.", ResultErrorCode.Validation);
		}

		issue.Labels.Add(normalised);
		issue.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to add label to issue {IssueId}: {Error}", request.IssueId, result.Error);
			return Result.Fail<IssueDto>(result.Error ?? "Failed to add label", result.ErrorCode);
		}

		var dto = new IssueDto(result.Value!);

		await _mediator.Publish(new IssueUpdatedEvent { Issue = dto }, cancellationToken);

		_logger.LogInformation("Successfully added label '{Label}' to issue {IssueId}", normalised, request.IssueId);
		return Result.Ok(dto);
	}
}
