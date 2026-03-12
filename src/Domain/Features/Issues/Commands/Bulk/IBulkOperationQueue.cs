// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IBulkOperationQueue.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Queue for processing bulk operations in the background.
/// </summary>
public interface IBulkOperationQueue
{
	/// <summary>
	///   Queues a bulk operation for background processing.
	/// </summary>
	/// <typeparam name="T">The type of command to queue.</typeparam>
	/// <param name="command">The command to process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An operation ID that can be used to check status.</returns>
	Task<string> QueueAsync<T>(T command, CancellationToken cancellationToken = default)
		where T : class;

	/// <summary>
	///   Gets the next queued operation, or null if queue is empty.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The next queued operation info.</returns>
	Task<QueuedBulkOperation?> DequeueAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///   Gets the status of a queued operation.
	/// </summary>
	/// <param name="operationId">The operation ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The operation status.</returns>
	Task<BulkOperationStatus?> GetStatusAsync(
		string operationId,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Updates the status of a queued operation.
	/// </summary>
	/// <param name="operationId">The operation ID.</param>
	/// <param name="status">The new status.</param>
	/// <param name="result">Optional result if completed.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task UpdateStatusAsync(
		string operationId,
		BulkOperationStatus status,
		BulkOperationResult? result = null,
		CancellationToken cancellationToken = default);
}

/// <summary>
///   Represents a queued bulk operation.
/// </summary>
/// <param name="OperationId">Unique identifier for the operation.</param>
/// <param name="Command">The command to execute.</param>
/// <param name="CommandType">The type name of the command.</param>
/// <param name="QueuedAt">When the operation was queued.</param>
public record QueuedBulkOperation(
	string OperationId,
	object Command,
	string CommandType,
	DateTime QueuedAt);

/// <summary>
///   Status of a bulk operation.
/// </summary>
public enum BulkOperationStatus
{
	/// <summary>
	///   Operation is queued and waiting to be processed.
	/// </summary>
	Queued,

	/// <summary>
	///   Operation is currently being processed.
	/// </summary>
	Processing,

	/// <summary>
	///   Operation completed successfully.
	/// </summary>
	Completed,

	/// <summary>
	///   Operation failed.
	/// </summary>
	Failed
}
