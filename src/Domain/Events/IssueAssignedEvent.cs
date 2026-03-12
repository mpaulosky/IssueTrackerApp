// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueAssignedEvent.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using MongoDB.Bson;

namespace Domain.Events;

/// <summary>
///   Event raised when an issue is assigned to a user.
/// </summary>
public sealed record IssueAssignedEvent : INotification
{
	/// <summary>
	///   The ID of the issue that was assigned.
	/// </summary>
	public required ObjectId IssueId { get; init; }

	/// <summary>
	///   The user identifier the issue was assigned to.
	/// </summary>
	public required string Assignee { get; init; }

	/// <summary>
	///   The issue title.
	/// </summary>
	public required string IssueTitle { get; init; }

	/// <summary>
	///   Timestamp when the event occurred.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
