// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     EmailQueueItem.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

/// <summary>
///   Represents an email queued for sending.
/// </summary>
public sealed class EmailQueueItem
{
	/// <summary>
	///   Unique identifier for the queue item.
	/// </summary>
	[BsonId]
	public ObjectId Id { get; set; }

	/// <summary>
	///   Recipient email address.
	/// </summary>
	public required string ToEmail { get; init; }

	/// <summary>
	///   Email subject.
	/// </summary>
	public required string Subject { get; init; }

	/// <summary>
	///   Email body (HTML or plain text).
	/// </summary>
	public required string Body { get; init; }

	/// <summary>
	///   Whether the body is HTML.
	/// </summary>
	public bool IsHtml { get; init; } = true;

	/// <summary>
	///   Optional sender name.
	/// </summary>
	public string? FromName { get; init; }

	/// <summary>
	///   Number of send attempts.
	/// </summary>
	public int Attempts { get; set; }

	/// <summary>
	///   Maximum number of send attempts before giving up.
	/// </summary>
	public int MaxAttempts { get; init; } = 3;

	/// <summary>
	///   When the email was queued.
	/// </summary>
	public DateTime QueuedAt { get; init; } = DateTime.UtcNow;

	/// <summary>
	///   When the next send attempt should be made.
	/// </summary>
	public DateTime? NextAttemptAt { get; set; }

	/// <summary>
	///   When the email was successfully sent.
	/// </summary>
	public DateTime? SentAt { get; set; }

	/// <summary>
	///   Last error message if send failed.
	/// </summary>
	public string? LastError { get; set; }

	/// <summary>
	///   Status of the email.
	/// </summary>
	public EmailQueueStatus Status { get; set; } = EmailQueueStatus.Pending;
}

/// <summary>
///   Status of an email in the queue.
/// </summary>
public enum EmailQueueStatus
{
	Pending,
	Sending,
	Sent,
	Failed
}
