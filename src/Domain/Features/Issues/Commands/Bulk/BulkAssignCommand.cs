// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkAssignCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Mappers;

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Command to assign multiple issues to a user.
/// </summary>
public record BulkAssignCommand(
	List<string> IssueIds,
	UserDto Assignee,
	string RequestedBy) : IRequest<Result<BulkOperationResult>>;

/// <summary>
///   Handler for bulk assignment operations.
/// </summary>
public sealed class BulkAssignCommandHandler : IRequestHandler<BulkAssignCommand, Result<BulkOperationResult>>
{
	private readonly IRepository<Issue> _repository;
	private readonly INotificationService _notificationService;
	private readonly ILogger<BulkAssignCommandHandler> _logger;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IUndoService _undoService;

	public BulkAssignCommandHandler(
		IRepository<Issue> repository,
		INotificationService notificationService,
		ILogger<BulkAssignCommandHandler> logger,
		IBulkOperationQueue bulkQueue,
		IUndoService undoService)
	{
		_repository = repository;
		_notificationService = notificationService;
		_logger = logger;
		_bulkQueue = bulkQueue;
		_undoService = undoService;
	}

	public async Task<Result<BulkOperationResult>> Handle(
		BulkAssignCommand request,
		CancellationToken cancellationToken)
	{
		if (request.IssueIds.Count == 0)
		{
			return Result.Fail<BulkOperationResult>("No issues specified for bulk assignment.");
		}

		if (request.IssueIds.Count > BulkOperationConstants.MaxBatchSize)
		{
			return Result.Fail<BulkOperationResult>(
				$"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize} items.");
		}

		_logger.LogInformation(
			"Processing bulk assignment for {Count} issues to user {UserId}",
			request.IssueIds.Count,
			request.Assignee.Id);

		// Queue for background processing if above threshold
		if (request.IssueIds.Count > BulkOperationConstants.BackgroundThreshold)
		{
			var operationId = await _bulkQueue.QueueAsync(request, cancellationToken);
			return Result.Ok(BulkOperationResult.Queued(request.IssueIds.Count, operationId));
		}

		return await ProcessBulkAssignAsync(request, cancellationToken);
	}

	internal async Task<Result<BulkOperationResult>> ProcessBulkAssignAsync(
		BulkAssignCommand request,
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

				// Store snapshot for undo — capture current Assignee before overwriting
				undoSnapshots.Add(new IssueUndoSnapshot(
					issue.Id.ToString(),
					BulkOperationType.Assignment,
					new AssignmentSnapshot(UserMapper.ToDto(issue.Assignee))));

				issue.Assignee = UserMapper.ToInfo(request.Assignee);
				issue.DateModified = DateTime.UtcNow;

				var updateResult = await _repository.UpdateAsync(issue, cancellationToken);

				if (updateResult.Failure)
				{
					errors.Add(new BulkOperationError(issueId, updateResult.Error ?? "Assignment failed"));
					continue;
				}

				// Notify assignee
				await _notificationService.NotifyIssueAssignedAsync(
					issue.Id,
					issue.Title,
					request.Assignee.Id,
					cancellationToken);

				successCount++;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error assigning issue {IssueId}", issueId);
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
			"Bulk assignment completed: {Success} succeeded, {Failed} failed",
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
