// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkOperationService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Issues.Commands.Bulk;
using MediatR;

namespace Web.Services;

/// <summary>
/// Progress update for bulk operations.
/// </summary>
public record BulkOperationProgress
{
	public required int TotalCount { get; init; }
	public required int ProcessedCount { get; init; }
	public required int SuccessCount { get; init; }
	public required int FailureCount { get; init; }
	public double Percentage => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;
}

/// <summary>
/// Service for executing bulk operations on issues.
/// </summary>
public interface IBulkOperationService
{
	/// <summary>
	/// Bulk updates the status of multiple issues.
	/// </summary>
	Task<Result<BulkOperationResult>> BulkUpdateStatusAsync(
		IEnumerable<string> issueIds,
		StatusDto status,
		string requestedBy,
		IProgress<BulkOperationProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Bulk updates the category of multiple issues.
	/// </summary>
	Task<Result<BulkOperationResult>> BulkUpdateCategoryAsync(
		IEnumerable<string> issueIds,
		CategoryDto category,
		string requestedBy,
		IProgress<BulkOperationProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Bulk assigns multiple issues to a user.
	/// </summary>
	Task<Result<BulkOperationResult>> BulkAssignAsync(
		IEnumerable<string> issueIds,
		UserDto assignee,
		string requestedBy,
		IProgress<BulkOperationProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Bulk deletes (archives) multiple issues.
	/// </summary>
	Task<Result<BulkOperationResult>> BulkDeleteAsync(
		IEnumerable<string> issueIds,
		UserDto archivedBy,
		string requestedBy,
		IProgress<BulkOperationProgress>? progress = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Bulk exports multiple issues to CSV.
	/// </summary>
	Task<Result<BulkExportResult>> BulkExportAsync(
		IEnumerable<string> issueIds,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Undoes the last bulk operation if an undo token is available.
	/// </summary>
	Task<Result<BulkOperationResult>> UndoLastOperationAsync(
		string undoToken,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the status of a background bulk operation.
	/// </summary>
	Task<BulkOperationStatus?> GetOperationStatusAsync(
		string operationId,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IBulkOperationService using MediatR commands.
/// </summary>
public sealed class BulkOperationService : IBulkOperationService
{
	private readonly IMediator _mediator;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly ILogger<BulkOperationService> _logger;

	public BulkOperationService(
		IMediator mediator,
		IBulkOperationQueue bulkQueue,
		ILogger<BulkOperationService> logger)
	{
		_mediator = mediator;
		_bulkQueue = bulkQueue;
		_logger = logger;
	}

	public async Task<Result<BulkOperationResult>> BulkUpdateStatusAsync(
		IEnumerable<string> issueIds,
		StatusDto status,
		string requestedBy,
		IProgress<BulkOperationProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		var ids = issueIds.ToList();

		_logger.LogInformation(
			"Executing bulk status update for {Count} issues",
			ids.Count);

		var command = new BulkUpdateStatusCommand(ids, status, requestedBy);
		var result = await _mediator.Send(command, cancellationToken);

		// Report final progress
		if (result.Success && result.Value is not null)
		{
			progress?.Report(new BulkOperationProgress
			{
				TotalCount = result.Value.TotalRequested,
				ProcessedCount = result.Value.SuccessCount + result.Value.FailureCount,
				SuccessCount = result.Value.SuccessCount,
				FailureCount = result.Value.FailureCount
			});
		}

		return result;
	}

	public async Task<Result<BulkOperationResult>> BulkUpdateCategoryAsync(
		IEnumerable<string> issueIds,
		CategoryDto category,
		string requestedBy,
		IProgress<BulkOperationProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		var ids = issueIds.ToList();

		_logger.LogInformation(
			"Executing bulk category update for {Count} issues",
			ids.Count);

		var command = new BulkUpdateCategoryCommand(ids, category, requestedBy);
		var result = await _mediator.Send(command, cancellationToken);

		// Report final progress
		if (result.Success && result.Value is not null)
		{
			progress?.Report(new BulkOperationProgress
			{
				TotalCount = result.Value.TotalRequested,
				ProcessedCount = result.Value.SuccessCount + result.Value.FailureCount,
				SuccessCount = result.Value.SuccessCount,
				FailureCount = result.Value.FailureCount
			});
		}

		return result;
	}

	public async Task<Result<BulkOperationResult>> BulkAssignAsync(
		IEnumerable<string> issueIds,
		UserDto assignee,
		string requestedBy,
		IProgress<BulkOperationProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		var ids = issueIds.ToList();

		_logger.LogInformation(
			"Executing bulk assignment for {Count} issues to user {UserId}",
			ids.Count,
			assignee.Id);

		var command = new BulkAssignCommand(ids, assignee, requestedBy);
		var result = await _mediator.Send(command, cancellationToken);

		// Report final progress
		if (result.Success && result.Value is not null)
		{
			progress?.Report(new BulkOperationProgress
			{
				TotalCount = result.Value.TotalRequested,
				ProcessedCount = result.Value.SuccessCount + result.Value.FailureCount,
				SuccessCount = result.Value.SuccessCount,
				FailureCount = result.Value.FailureCount
			});
		}

		return result;
	}

	public async Task<Result<BulkOperationResult>> BulkDeleteAsync(
		IEnumerable<string> issueIds,
		UserDto archivedBy,
		string requestedBy,
		IProgress<BulkOperationProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		var ids = issueIds.ToList();

		_logger.LogInformation(
			"Executing bulk delete for {Count} issues",
			ids.Count);

		var command = new BulkDeleteCommand(ids, archivedBy, requestedBy);
		var result = await _mediator.Send(command, cancellationToken);

		// Report final progress
		if (result.Success && result.Value is not null)
		{
			progress?.Report(new BulkOperationProgress
			{
				TotalCount = result.Value.TotalRequested,
				ProcessedCount = result.Value.SuccessCount + result.Value.FailureCount,
				SuccessCount = result.Value.SuccessCount,
				FailureCount = result.Value.FailureCount
			});
		}

		return result;
	}

	public async Task<Result<BulkExportResult>> BulkExportAsync(
		IEnumerable<string> issueIds,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var ids = issueIds.ToList();

		_logger.LogInformation(
			"Executing bulk export for {Count} issues",
			ids.Count);

		var command = new BulkExportCommand(ids, requestedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<BulkOperationResult>> UndoLastOperationAsync(
		string undoToken,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		_logger.LogInformation(
			"Executing undo for token {Token}",
			undoToken);

		var command = new UndoBulkOperationCommand(undoToken, requestedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<BulkOperationStatus?> GetOperationStatusAsync(
		string operationId,
		CancellationToken cancellationToken = default)
	{
		return await _bulkQueue.GetStatusAsync(operationId, cancellationToken);
	}
}
