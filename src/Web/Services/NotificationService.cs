// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     NotificationService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Events;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using Web.Hubs;

namespace Web.Services;

/// <summary>
///   Implementation of INotificationService using SignalR.
/// </summary>
public sealed class NotificationService : INotificationService
{
	private readonly IHubContext<IssueHub> _hubContext;
	private readonly ILogger<NotificationService> _logger;

	public NotificationService(IHubContext<IssueHub> hubContext, ILogger<NotificationService> logger)
	{
		_hubContext = hubContext;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task NotifyIssueCreatedAsync(IssueDto issue, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Notifying clients of new issue created: {IssueId}", issue.Id);

		var evt = new IssueCreatedEvent { Issue = issue };

		// Notify all connected clients
		await _hubContext.Clients.Group("all").SendAsync("IssueCreated", evt, cancellationToken);
	}

	/// <inheritdoc />
	public async Task NotifyIssueUpdatedAsync(IssueDto issue, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Notifying clients of issue updated: {IssueId}", issue.Id);

		var evt = new IssueUpdatedEvent { Issue = issue };

		// Notify clients in the issue-specific group
		await _hubContext.Clients.Group($"issue-{issue.Id}").SendAsync("IssueUpdated", evt, cancellationToken);

		// Also notify all clients for list updates
		await _hubContext.Clients.Group("all").SendAsync("IssueUpdated", evt, cancellationToken);
	}

	/// <inheritdoc />
	public async Task NotifyCommentAddedAsync(ObjectId issueId, string issueTitle, string issueOwner, CommentDto comment, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Notifying clients of comment added to issue: {IssueId}", issueId);

		var evt = new CommentAddedEvent 
		{ 
			IssueId = issueId, 
			IssueTitle = issueTitle,
			IssueOwner = issueOwner,
			Comment = comment 
		};

		// Notify clients in the issue-specific group
		await _hubContext.Clients.Group($"issue-{issueId}").SendAsync("CommentAdded", evt, cancellationToken);
	}

	/// <inheritdoc />
	public async Task NotifyIssueAssignedAsync(ObjectId issueId, string issueTitle, string assignee, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Notifying clients of issue assigned: {IssueId} to {Assignee}", issueId, assignee);

		var evt = new IssueAssignedEvent 
		{ 
			IssueId = issueId, 
			IssueTitle = issueTitle,
			Assignee = assignee 
		};

		// Notify clients in the issue-specific group
		await _hubContext.Clients.Group($"issue-{issueId}").SendAsync("IssueAssigned", evt, cancellationToken);

		// Also notify all clients for list updates
		await _hubContext.Clients.Group("all").SendAsync("IssueAssigned", evt, cancellationToken);
	}
}
