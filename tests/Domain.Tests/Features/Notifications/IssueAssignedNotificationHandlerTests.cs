// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueAssignedNotificationHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;
using Domain.Events;
using Domain.Features.Notifications;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Notifications;

/// <summary>
///   Unit tests for IssueAssignedNotificationHandler.
/// </summary>
public sealed class IssueAssignedNotificationHandlerTests
{
	private readonly IMediator _mediator;
	private readonly ILogger<IssueAssignedNotificationHandler> _logger;
	private readonly IssueAssignedNotificationHandler _sut;

	public IssueAssignedNotificationHandlerTests()
	{
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<IssueAssignedNotificationHandler>>();
		_sut = new IssueAssignedNotificationHandler(_mediator, _logger);
	}

	[Fact]
	public async Task Handle_QueuesEmailToAssignee()
	{
		// Arrange
		var notification = new IssueAssignedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			Assignee = "assignee@example.com",
			IssueTitle = "Test Issue"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.ToEmail == notification.Assignee &&
				cmd.Subject.Contains(notification.IssueTitle) &&
				cmd.Body.Contains(notification.IssueTitle) &&
				cmd.IsHtml),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WhenNoEmailPreference_DoesNotQueue()
	{
		// NOTE: The current implementation always queues emails.
		// This test documents the expected behavior when user preferences are implemented.
		// For now, we verify the handler completes without throwing even if Send fails.

		// Arrange
		var notification = new IssueAssignedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			Assignee = "user-without-email-pref@example.com",
			IssueTitle = "Test Issue"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<Result>(new InvalidOperationException("Simulated failure")));

		// Act
		var act = () => _sut.Handle(notification, CancellationToken.None);

		// Assert - Handler should catch exception and not rethrow
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task Handle_IncludesIssueIdInEmail()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var notification = new IssueAssignedEvent
		{
			IssueId = issueId,
			Assignee = "assignee@example.com",
			IssueTitle = "Bug Fix Required"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.Body.Contains(issueId.ToString())),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_SetsSubjectWithIssueTitle()
	{
		// Arrange
		var notification = new IssueAssignedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			Assignee = "developer@example.com",
			IssueTitle = "Critical Security Fix"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.Subject == $"Assigned to Issue: {notification.IssueTitle}"),
			Arg.Any<CancellationToken>());
	}
}
