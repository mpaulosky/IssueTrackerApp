// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkUpdateStatusCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Events;
using Domain.Mappers;

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Maximum allowed batch size for bulk operations.
/// </summary>
public static class BulkOperationConstants
{
	public const int MaxBatchSize = 100;
	public const int BackgroundThreshold = 50;
}

/// <summary>
///   Command to change the status of multiple issues.
/// </summary>
public record BulkUpdateStatusCommand(
	List<string> IssueIds,
	StatusDto NewStatus,
	string RequestedBy) : IRequest<Result<BulkOperationResult>>;

/// <summary>
///   Handler for bulk status updates.
/// </summary>
public sealed class BulkUpdateStatusCommandHandler : IRequestHandler<BulkUpdateStatusCommand, Result<BulkOperationResult>>
{
	private readonly IRepository<Issue> _repository;
	private readonly IMediator _mediator;
	private readonly ILogger<BulkUpdateStatusCommandHandler> _logger;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IUndoService _undoService;

	public BulkUpdateStatusCommandHandler(
		IRepository<Issue> repository,
		IMediator mediator,
		ILogger<BulkUpdateStatusCommandHandler> logger,
		IBulkOperationQueue bulkQueue,
		IUndoService undoService)
	{
		_repository = repository;
		_mediator = mediator;
		_logger = logger;
		_bulkQueue = bulkQueue;
		_undoService = undoService;
	}

	public async Task<Result<BulkOperationResult>> Handle(
		BulkUpdateStatusCommand request,
		CancellationToken cancellationToken)
	{
		if (request.IssueIds.Count == 0)
		{
			return Result.Fail<BulkOperationResult>("No issues specified for bulk update.");
		}

		if (request.IssueIds.Count > BulkOperationConstants.MaxBatchSize)
		{
			return Result.Fail<BulkOperationResult>(
				$"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize} items.");
		}

		_logger.LogInformation(
			"Processing bulk status update for {Count} issues to status {Status}",
			request.IssueIds.Count,
			request.NewStatus.StatusName);

		// Queue for background processing if above threshold
		if (request.IssueIds.Count > BulkOperationConstants.BackgroundThreshold)
		{
			var operationId = await _bulkQueue.QueueAsync(request, cancellationToken);
			return Result.Ok(BulkOperationResult.Queued(request.IssueIds.Count, operationId));
		}

		return await ProcessBulkStatusUpdateAsync(request, cancellationToken);
	}

	internal async Task<Result<BulkOperationResult>> ProcessBulkStatusUpdateAsync(
		BulkUpdateStatusCommand request,
		CancellationToken cancellationToken)
	{
		var errors = new List<BulkOperationError>();
		var successCount = 0;
		var undoSnapshots = new List<IssueUndoSnapshot>();

		foreach (var issueId in request.IssueIds)
		{
			try
			{
				var existingResult = await _repository.GetByIdAsync(issueId, cancellationToken);

				if (existingResult.Failure || existingResult.Value is null)
				{
					errors.Add(new BulkOperationError(issueId, "Issue not found"));
					continue;
				}

				var issue = existingResult.Value;

				// Store snapshot for undo
				undoSnapshots.Add(new IssueUndoSnapshot(
					issue.Id.ToString(),
					BulkOperationType.StatusUpdate,
					new StatusUpdateSnapshot(StatusMapper.ToDto(issue.Status))));

				var oldStatus = issue.Status.StatusName;
				issue.Status = StatusMapper.ToInfo(request.NewStatus);
				issue.DateModified = DateTime.UtcNow;

				var updateResult = await _repository.UpdateAsync(issue, cancellationToken);

				if (updateResult.Failure)
				{
					errors.Add(new BulkOperationError(issueId, updateResult.Error ?? "Update failed"));
					continue;
				}

				// Publish event for notifications
				await _mediator.Publish(new IssueStatusChangedEvent
				{
					IssueId = issue.Id,
					IssueTitle = issue.Title,
					OldStatus = oldStatus,
					NewStatus = request.NewStatus.StatusName,
					IssueOwner = issue.Author.Id
				}, cancellationToken);

				successCount++;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating issue {IssueId}", issueId);
				errors.Add(new BulkOperationError(issueId, ex.Message));
			}
		}

		// Store undo data if any succeeded
		string? undoToken = null;
		if (successCount > 0)
		{
			undoToken = await _undoService.StoreUndoDataAsync(
				request.RequestedBy,
				undoSnapshots.Where(s => !errors.Any(e => e.IssueId == s.IssueId)).ToList(),
				cancellationToken);
		}

		_logger.LogInformation(
			"Bulk status update completed: {Success} succeeded, {Failed} failed",
			successCount,
			errors.Count);

		return Result.Ok(new BulkOperationResult(
			request.IssueIds.Count,
			successCount,
			errors.Count,
			errors,
			undoToken));
	}
}
