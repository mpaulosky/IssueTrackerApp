// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkDeleteCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Mappers;

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Command to delete (archive) multiple issues. Admin only operation.
/// </summary>
public record BulkDeleteCommand(
	List<string> IssueIds,
	UserDto DeletedBy,
	string RequestedBy) : IRequest<Result<BulkOperationResult>>;

/// <summary>
///   Handler for bulk delete (archive) operations.
/// </summary>
public sealed class BulkDeleteCommandHandler : IRequestHandler<BulkDeleteCommand, Result<BulkOperationResult>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<BulkDeleteCommandHandler> _logger;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IUndoService _undoService;

	public BulkDeleteCommandHandler(
		IRepository<Issue> repository,
		ILogger<BulkDeleteCommandHandler> logger,
		IBulkOperationQueue bulkQueue,
		IUndoService undoService)
	{
		_repository = repository;
		_logger = logger;
		_bulkQueue = bulkQueue;
		_undoService = undoService;
	}

	public async Task<Result<BulkOperationResult>> Handle(
		BulkDeleteCommand request,
		CancellationToken cancellationToken)
	{
		if (request.IssueIds.Count == 0)
		{
			return Result.Fail<BulkOperationResult>("No issues specified for bulk delete.");
		}

		if (request.IssueIds.Count > BulkOperationConstants.MaxBatchSize)
		{
			return Result.Fail<BulkOperationResult>(
				$"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize} items.");
		}

		_logger.LogInformation(
			"Processing bulk delete for {Count} issues by user {UserId}",
			request.IssueIds.Count,
			request.DeletedBy.Id);

		// Queue for background processing if above threshold
		if (request.IssueIds.Count > BulkOperationConstants.BackgroundThreshold)
		{
			var operationId = await _bulkQueue.QueueAsync(request, cancellationToken);
			return Result.Ok(BulkOperationResult.Queued(request.IssueIds.Count, operationId));
		}

		return await ProcessBulkDeleteAsync(request, cancellationToken);
	}

	internal async Task<Result<BulkOperationResult>> ProcessBulkDeleteAsync(
		BulkDeleteCommand request,
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

				if (issue.Archived)
				{
					errors.Add(new BulkOperationError(issueId, "Issue is already archived"));
					continue;
				}

				// Store snapshot for undo
				undoSnapshots.Add(new IssueUndoSnapshot(
					issue.Id.ToString(),
					BulkOperationType.Delete,
					new DeleteSnapshot(issue.Archived, UserMapper.ToDto(issue.ArchivedBy))));

				issue.Archived = true;
				issue.ArchivedBy = UserMapper.ToInfo(request.DeletedBy);
				issue.DateModified = DateTime.UtcNow;

				var updateResult = await _repository.UpdateAsync(issue, cancellationToken);

				if (updateResult.Failure)
				{
					errors.Add(new BulkOperationError(issueId, updateResult.Error ?? "Delete failed"));
					continue;
				}

				successCount++;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting issue {IssueId}", issueId);
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
			"Bulk delete completed: {Success} succeeded, {Failed} failed",
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
