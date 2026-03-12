// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentAddedNotificationHandler.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Events;

namespace Domain.Features.Notifications;

/// <summary>
///   Handles CommentAddedEvent and queues email notifications.
/// </summary>
public sealed class CommentAddedNotificationHandler : INotificationHandler<CommentAddedEvent>
{
	private readonly IMediator _mediator;
	private readonly ILogger<CommentAddedNotificationHandler> _logger;

	public CommentAddedNotificationHandler(
		IMediator mediator,
		ILogger<CommentAddedNotificationHandler> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	public async Task Handle(CommentAddedEvent notification, CancellationToken cancellationToken)
	{
		try
		{
			// Don't send email to the comment author
			if (notification.Comment.Author.Id == notification.IssueOwner)
			{
				_logger.LogInformation("Skipping email notification - comment author is issue owner");
				return;
			}

			// NOTE: In a real app, you would check user preferences from Auth0 metadata or local storage
			// For now, we'll assume the user wants email notifications

			// Email body with simple HTML formatting
			var emailBody = $@"
				<html>
				<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
					<div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
						<h2 style='color: #333;'>New Comment on Your Issue</h2>
						<p>Hello,</p>
						<p>A new comment has been added to your issue:</p>
						<div style='background-color: white; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0;'>
							<h3 style='margin-top: 0;'>{notification.IssueTitle}</h3>
							<p><strong>Comment by:</strong> {notification.Comment.Author.Name}</p>
							<div style='background-color: #f8f9fa; padding: 10px; border-radius: 3px; margin-top: 10px;'>
								<p style='margin: 0;'>{notification.Comment.Description}</p>
							</div>
						</div>
						<p>Click here to view the full discussion.</p>
						<p style='color: #666; font-size: 12px; margin-top: 30px;'>
							This is an automated notification from IssueTracker.
						</p>
					</div>
				</body>
				</html>";

			var queueEmailCommand = new QueueEmailCommand
			{
				ToEmail = notification.IssueOwner,
				Subject = $"New Comment on Issue: {notification.IssueTitle}",
				Body = emailBody,
				IsHtml = true
			};

			await _mediator.Send(queueEmailCommand, cancellationToken);

			_logger.LogInformation("Queued comment notification email for {IssueOwner}", notification.IssueOwner);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to handle CommentAddedEvent for issue owner {IssueOwner}", notification.IssueOwner);
		}
	}
}
