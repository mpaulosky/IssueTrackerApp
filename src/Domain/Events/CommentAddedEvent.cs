// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentAddedEvent.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.DTOs;
using MongoDB.Bson;

namespace Domain.Events;

/// <summary>
///   Event raised when a comment is added to an issue.
/// </summary>
public sealed record CommentAddedEvent
{
	/// <summary>
	///   The ID of the issue the comment was added to.
	/// </summary>
	public required ObjectId IssueId { get; init; }

	/// <summary>
	///   The newly added comment.
	/// </summary>
	public required CommentDto Comment { get; init; }

	/// <summary>
	///   Timestamp when the event occurred.
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
