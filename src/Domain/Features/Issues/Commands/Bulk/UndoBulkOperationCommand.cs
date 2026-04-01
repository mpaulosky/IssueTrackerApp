// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UndoBulkOperationCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Mappers;

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Command to undo a previous bulk operation.
/// </summary>
public record UndoBulkOperationCommand(
	string UndoToken,
	string RequestedBy) : IRequest<Result<BulkOperationResult>>;

/// <summary>
///   Handler for undoing bulk operations.
/// </summary>
public sealed class UndoBulkOperationCommandHandler : IRequestHandler<UndoBulkOperationCommand, Result<BulkOperationResult>>
{
	private readonly IRepository<Issue> _repository;
	private readonly IUndoService _undoService;
	private readonly ILogger<UndoBulkOperationCommandHandler> _logger;

	public UndoBulkOperationCommandHandler(
		IRepository<Issue> repository,
		IUndoService undoService,
		ILogger<UndoBulkOperationCommandHandler> logger)
	{
		_repository = repository;
		_undoService = undoService;
		_logger = logger;
	}

	public async Task<Result<BulkOperationResult>> Handle(
		UndoBulkOperationCommand request,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.UndoToken))
		{
			return Result.Fail<BulkOperationResult>("Invalid undo token.");
		}

		var undoData = await _undoService.GetUndoDataAsync(
			request.UndoToken,
			request.RequestedBy,
			cancellationToken);

		if (undoData is null)
		{
			return Result.Fail<BulkOperationResult>(
				"Undo token not found, expired, or belongs to another user.");
		}

		_logger.LogInformation(
			"Processing undo for {Count} issues",
			undoData.Snapshots.Count);

		var errors = new List<BulkOperationError>();
		var successCount = 0;

		foreach (var snapshot in undoData.Snapshots)
		{
			try
			{
				var existingResult = await _repository.GetByIdAsync(snapshot.IssueId, cancellationToken);

				if (existingResult.Failure || existingResult.Value is null)
				{
					errors.Add(new BulkOperationError(snapshot.IssueId, "Issue not found"));
					continue;
				}

				var issue = existingResult.Value;

				// Restore previous state based on operation type
				switch (snapshot.OperationType)
				{
					case BulkOperationType.StatusUpdate:
						if (snapshot.PreviousState is StatusUpdateSnapshot statusSnapshot)
						{
							issue.Status = StatusMapper.ToInfo(statusSnapshot.PreviousStatus);
						}
						break;

					case BulkOperationType.CategoryUpdate:
						if (snapshot.PreviousState is CategoryUpdateSnapshot categorySnapshot)
						{
							issue.Category = CategoryMapper.ToInfo(categorySnapshot.PreviousCategory);
						}
						break;

					case BulkOperationType.Assignment:
						if (snapshot.PreviousState is AssignmentSnapshot assignmentSnapshot)
						{
							issue.Assignee = UserMapper.ToInfo(assignmentSnapshot.PreviousAssignee);
						}
						break;

					case BulkOperationType.Delete:
						if (snapshot.PreviousState is DeleteSnapshot deleteSnapshot)
						{
							issue.Archived = deleteSnapshot.WasArchived;
							issue.ArchivedBy = UserMapper.ToInfo(deleteSnapshot.ArchivedBy);
						}
						break;
				}

				issue.DateModified = DateTime.UtcNow;

				var updateResult = await _repository.UpdateAsync(issue, cancellationToken);

				if (updateResult.Failure)
				{
					errors.Add(new BulkOperationError(snapshot.IssueId, updateResult.Error ?? "Undo failed"));
					continue;
				}

				successCount++;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error undoing operation for issue {IssueId}", snapshot.IssueId);
				errors.Add(new BulkOperationError(snapshot.IssueId, ex.Message));
			}
		}

		// Invalidate the undo token after use
		await _undoService.InvalidateUndoTokenAsync(request.UndoToken, cancellationToken);

		_logger.LogInformation(
			"Undo completed: {Success} succeeded, {Failed} failed",
			successCount,
			errors.Count);

		return Result.Ok(new BulkOperationResult(
			undoData.Snapshots.Count,
			successCount,
			errors.Count,
			errors));
	}
}
