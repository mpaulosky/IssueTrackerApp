// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueStatusChangedNotificationHandler.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Events;

namespace Domain.Features.Notifications;

/// <summary>
///   Handles IssueStatusChangedEvent and queues email notifications.
/// </summary>
public sealed class IssueStatusChangedNotificationHandler : INotificationHandler<IssueStatusChangedEvent>
{
	private readonly IMediator _mediator;
	private readonly ILogger<IssueStatusChangedNotificationHandler> _logger;

	public IssueStatusChangedNotificationHandler(
		IMediator mediator,
		ILogger<IssueStatusChangedNotificationHandler> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	public async Task Handle(IssueStatusChangedEvent notification, CancellationToken cancellationToken)
	{
		try
		{
			// NOTE: In a real app, you would check user preferences from Auth0 metadata or local storage
			// For now, we'll assume the user wants email notifications

			// Email body with simple HTML formatting
			var emailBody = $@"
				<html>
				<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
					<div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
						<h2 style='color: #333;'>Issue Status Changed</h2>
						<p>Hello,</p>
						<p>The status of your issue has been updated:</p>
						<div style='background-color: white; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0;'>
							<h3 style='margin-top: 0;'>{notification.IssueTitle}</h3>
							<p><strong>Previous Status:</strong> <span style='color: #dc3545;'>{notification.OldStatus}</span></p>
							<p><strong>New Status:</strong> <span style='color: #28a745;'>{notification.NewStatus}</span></p>
						</div>
						<p>Click here to view the issue details.</p>
						<p style='color: #666; font-size: 12px; margin-top: 30px;'>
							This is an automated notification from IssueTracker.
						</p>
					</div>
				</body>
				</html>";

			var queueEmailCommand = new QueueEmailCommand
			{
				ToEmail = notification.IssueOwner,
				Subject = $"Status Changed: {notification.IssueTitle} is now {notification.NewStatus}",
				Body = emailBody,
				IsHtml = true
			};

			await _mediator.Send(queueEmailCommand, cancellationToken);

			_logger.LogInformation("Queued status change notification email for {IssueOwner}", notification.IssueOwner);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to handle IssueStatusChangedEvent for issue owner {IssueOwner}", notification.IssueOwner);
		}
	}
}
