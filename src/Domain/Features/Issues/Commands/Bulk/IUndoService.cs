// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IUndoService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Service for storing and retrieving undo data for bulk operations.
/// </summary>
public interface IUndoService
{
	/// <summary>
	///   Stores undo data and returns a token that can be used to retrieve it.
	/// </summary>
	/// <param name="requestedBy">The user who requested the operation.</param>
	/// <param name="snapshots">The snapshots to store for undo.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A token that can be used to retrieve the undo data.</returns>
	Task<string> StoreUndoDataAsync(
		string requestedBy,
		List<IssueUndoSnapshot> snapshots,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Retrieves undo data for a given token.
	/// </summary>
	/// <param name="undoToken">The token returned from StoreUndoDataAsync.</param>
	/// <param name="requestedBy">The user requesting the undo (must match original requester).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The stored undo data, or null if not found or expired.</returns>
	Task<UndoData?> GetUndoDataAsync(
		string undoToken,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Removes undo data for a given token (called after successful undo).
	/// </summary>
	/// <param name="undoToken">The token to invalidate.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task InvalidateUndoTokenAsync(
		string undoToken,
		CancellationToken cancellationToken = default);
}

/// <summary>
///   Container for undo operation data.
/// </summary>
/// <param name="RequestedBy">The user who performed the original operation.</param>
/// <param name="Snapshots">The issue snapshots for undo.</param>
/// <param name="CreatedAt">When the undo data was created.</param>
public record UndoData(
	string RequestedBy,
	List<IssueUndoSnapshot> Snapshots,
	DateTime CreatedAt);

/// <summary>
///   Snapshot of an issue for undo purposes.
/// </summary>
/// <param name="IssueId">The issue ID.</param>
/// <param name="OperationType">The type of operation performed.</param>
/// <param name="PreviousState">The previous state data.</param>
public record IssueUndoSnapshot(
	string IssueId,
	BulkOperationType OperationType,
	object PreviousState);

/// <summary>
///   Types of bulk operations that can be undone.
/// </summary>
public enum BulkOperationType
{
	StatusUpdate,
	CategoryUpdate,
	Assignment,
	Delete
}

/// <summary>
///   Snapshot of status for undo.
/// </summary>
public record StatusUpdateSnapshot(StatusDto PreviousStatus);

/// <summary>
///   Snapshot of category for undo.
/// </summary>
public record CategoryUpdateSnapshot(CategoryDto PreviousCategory);

/// <summary>
///   Snapshot of assignment for undo.
/// </summary>
public record AssignmentSnapshot(UserDto PreviousAssignee);

/// <summary>
///   Snapshot of delete state for undo.
/// </summary>
public record DeleteSnapshot(bool WasArchived, UserDto ArchivedBy);
