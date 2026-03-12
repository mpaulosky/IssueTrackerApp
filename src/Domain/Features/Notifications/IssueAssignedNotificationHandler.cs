// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueAssignedNotificationHandler.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Events;

namespace Domain.Features.Notifications;

/// <summary>
///   Handles IssueAssignedEvent and queues email notifications.
/// </summary>
public sealed class IssueAssignedNotificationHandler : INotificationHandler<IssueAssignedEvent>
{
	private readonly IMediator _mediator;
	private readonly ILogger<IssueAssignedNotificationHandler> _logger;

	public IssueAssignedNotificationHandler(
		IMediator mediator,
		ILogger<IssueAssignedNotificationHandler> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	public async Task Handle(IssueAssignedEvent notification, CancellationToken cancellationToken)
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
						<h2 style='color: #333;'>You've Been Assigned to an Issue</h2>
						<p>Hello,</p>
						<p>You have been assigned to the following issue:</p>
						<div style='background-color: white; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0;'>
							<h3 style='margin-top: 0;'>{notification.IssueTitle}</h3>
							<p><strong>Issue ID:</strong> {notification.IssueId}</p>
						</div>
						<p>Please review and take appropriate action.</p>
						<p style='color: #666; font-size: 12px; margin-top: 30px;'>
							This is an automated notification from IssueTracker.
						</p>
					</div>
				</body>
				</html>";

			var queueEmailCommand = new QueueEmailCommand
			{
				ToEmail = notification.Assignee,
				Subject = $"Assigned to Issue: {notification.IssueTitle}",
				Body = emailBody,
				IsHtml = true
			};

			await _mediator.Send(queueEmailCommand, cancellationToken);

			_logger.LogInformation("Queued assignment notification email for {Assignee}", notification.Assignee);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to handle IssueAssignedEvent for assignee {Assignee}", notification.Assignee);
		}
	}
}
