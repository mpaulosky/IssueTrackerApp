// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     INotificationService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.DTOs;

using MongoDB.Bson;

namespace Domain.Abstractions;

/// <summary>
///   Service for sending real-time notifications via SignalR.
/// </summary>
public interface INotificationService
{
	/// <summary>
	///   Notifies connected clients that a new issue was created.
	/// </summary>
	/// <param name="issue">The created issue.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task NotifyIssueCreatedAsync(IssueDto issue, CancellationToken cancellationToken = default);

	/// <summary>
	///   Notifies connected clients that an issue was updated.
	/// </summary>
	/// <param name="issue">The updated issue.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task NotifyIssueUpdatedAsync(IssueDto issue, CancellationToken cancellationToken = default);

	/// <summary>
	///   Notifies connected clients that a comment was added to an issue.
	/// </summary>
	/// <param name="issueId">The ID of the issue.</param>
	/// <param name="issueTitle">The title of the issue.</param>
	/// <param name="issueOwner">The owner of the issue.</param>
	/// <param name="comment">The added comment.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task NotifyCommentAddedAsync(ObjectId issueId, string issueTitle, string issueOwner, CommentDto comment, CancellationToken cancellationToken = default);

	/// <summary>
	///   Notifies connected clients that an issue was assigned to a user.
	/// </summary>
	/// <param name="issueId">The ID of the issue.</param>
	/// <param name="issueTitle">The title of the issue.</param>
	/// <param name="assignee">The user identifier the issue was assigned to.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task NotifyIssueAssignedAsync(ObjectId issueId, string issueTitle, string assignee, CancellationToken cancellationToken = default);
}
