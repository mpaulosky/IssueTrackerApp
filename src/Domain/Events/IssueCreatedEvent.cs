// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueCreatedEvent.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.DTOs;

namespace Domain.Events;

/// <summary>
///   Event raised when a new issue is created.
/// </summary>
public sealed record IssueCreatedEvent
{
	/// <summary>
	///   The newly created issue.
	/// </summary>
	public required IssueDto Issue { get; init; }

	/// <summary>
	///   Timestamp when the event occurred.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
