// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueStatusChangedEvent.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Events;

/// <summary>
///   Event raised when an issue's status changes.
/// </summary>
public sealed record IssueStatusChangedEvent : INotification
{
	/// <summary>
	///   The ID of the issue.
	/// </summary>
	public required ObjectId IssueId { get; init; }

	/// <summary>
	///   The issue title.
	/// </summary>
	public required string IssueTitle { get; init; }

	/// <summary>
	///   The old status.
	/// </summary>
	public required string OldStatus { get; init; }

	/// <summary>
	///   The new status.
	/// </summary>
	public required string NewStatus { get; init; }

	/// <summary>
	///   The user who owns the issue (to be notified).
	/// </summary>
	public required string IssueOwner { get; init; }

	/// <summary>
	///   Timestamp when the event occurred.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
