// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueUpdatedEvent.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.DTOs;

namespace Domain.Events;

/// <summary>
///   Event raised when an issue is updated.
/// </summary>
public sealed record IssueUpdatedEvent
{
	/// <summary>
	///   The updated issue.
	/// </summary>
	public required IssueDto Issue { get; init; }

	/// <summary>
	///   Timestamp when the event occurred.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
