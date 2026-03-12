// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkUpdateCategoryCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Command to change the category of multiple issues.
/// </summary>
public record BulkUpdateCategoryCommand(
	List<string> IssueIds,
	CategoryDto NewCategory,
	string RequestedBy) : IRequest<Result<BulkOperationResult>>;

/// <summary>
///   Handler for bulk category updates.
/// </summary>
public sealed class BulkUpdateCategoryCommandHandler : IRequestHandler<BulkUpdateCategoryCommand, Result<BulkOperationResult>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<BulkUpdateCategoryCommandHandler> _logger;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IUndoService _undoService;

	public BulkUpdateCategoryCommandHandler(
		IRepository<Issue> repository,
		ILogger<BulkUpdateCategoryCommandHandler> logger,
		IBulkOperationQueue bulkQueue,
		IUndoService undoService)
	{
		_repository = repository;
		_logger = logger;
		_bulkQueue = bulkQueue;
		_undoService = undoService;
	}

	public async Task<Result<BulkOperationResult>> Handle(
		BulkUpdateCategoryCommand request,
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
			"Processing bulk category update for {Count} issues to category {Category}",
			request.IssueIds.Count,
			request.NewCategory.CategoryName);

		// Queue for background processing if above threshold
		if (request.IssueIds.Count > BulkOperationConstants.BackgroundThreshold)
		{
			var operationId = await _bulkQueue.QueueAsync(request, cancellationToken);
			return Result.Ok(BulkOperationResult.Queued(request.IssueIds.Count, operationId));
		}

		return await ProcessBulkCategoryUpdateAsync(request, cancellationToken);
	}

	internal async Task<Result<BulkOperationResult>> ProcessBulkCategoryUpdateAsync(
		BulkUpdateCategoryCommand request,
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
					BulkOperationType.CategoryUpdate,
					new CategoryUpdateSnapshot(issue.Category)));

				issue.Category = request.NewCategory;
				issue.DateModified = DateTime.UtcNow;

				var updateResult = await _repository.UpdateAsync(issue, cancellationToken);

				if (updateResult.Failure)
				{
					errors.Add(new BulkOperationError(issueId, updateResult.Error ?? "Update failed"));
					continue;
				}

				successCount++;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating issue category {IssueId}", issueId);
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
			"Bulk category update completed: {Success} succeeded, {Failed} failed",
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
