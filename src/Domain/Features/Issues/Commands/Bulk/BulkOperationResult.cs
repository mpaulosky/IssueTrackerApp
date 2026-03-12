// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkOperationResult.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Represents the result of a bulk operation on issues.
/// </summary>
/// <param name="TotalRequested">Total number of items requested for the operation.</param>
/// <param name="SuccessCount">Number of items successfully processed.</param>
/// <param name="FailureCount">Number of items that failed to process.</param>
/// <param name="Errors">List of errors for failed items.</param>
/// <param name="UndoToken">Optional token to undo this bulk operation.</param>
/// <param name="OperationId">Optional operation ID for background processing.</param>
public record BulkOperationResult(
	int TotalRequested,
	int SuccessCount,
	int FailureCount,
	List<BulkOperationError> Errors,
	string? UndoToken = null,
	string? OperationId = null)
{
	/// <summary>
	///   Gets a value indicating whether all items were processed successfully.
	/// </summary>
	public bool IsFullSuccess => FailureCount == 0 && SuccessCount == TotalRequested;

	/// <summary>
	///   Gets a value indicating whether the operation is queued for background processing.
	/// </summary>
	public bool IsQueued => !string.IsNullOrEmpty(OperationId);

	/// <summary>
	///   Creates a successful result with all items processed.
	/// </summary>
	public static BulkOperationResult Success(int count, string? undoToken = null)
	{
		return new BulkOperationResult(count, count, 0, [], undoToken);
	}

	/// <summary>
	///   Creates a queued result for background processing.
	/// </summary>
	public static BulkOperationResult Queued(int count, string operationId)
	{
		return new BulkOperationResult(count, 0, 0, [], null, operationId);
	}
}

/// <summary>
///   Represents an error for a single item in a bulk operation.
/// </summary>
/// <param name="IssueId">The ID of the issue that failed.</param>
/// <param name="ErrorMessage">The error message describing the failure.</param>
public record BulkOperationError(string IssueId, string ErrorMessage);
