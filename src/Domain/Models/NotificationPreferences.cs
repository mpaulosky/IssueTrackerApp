// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     NotificationPreferences.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

/// <summary>
///   Represents a user's email notification preferences.
/// </summary>
public sealed record NotificationPreferences
{
	/// <summary>
	///   The user identifier (Auth0 ID).
	/// </summary>
	public required string UserId { get; init; }

	/// <summary>
	///   Whether to send email when assigned to an issue.
	/// </summary>
	public bool EmailOnAssigned { get; init; } = true;

	/// <summary>
	///   Whether to send email when a comment is added to an issue.
	/// </summary>
	public bool EmailOnComment { get; init; } = true;

	/// <summary>
	///   Whether to send email when issue status changes.
	/// </summary>
	public bool EmailOnStatusChange { get; init; } = true;

	/// <summary>
	///   Whether to send email when mentioned in an issue or comment.
	/// </summary>
	public bool EmailOnMention { get; init; } = true;
}
