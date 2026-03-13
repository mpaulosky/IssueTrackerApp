// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueStatusChangedNotificationHandlerTests.cs
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
///   Unit tests for IssueStatusChangedNotificationHandler.
/// </summary>
public sealed class IssueStatusChangedNotificationHandlerTests
{
	private readonly IMediator _mediator;
	private readonly ILogger<IssueStatusChangedNotificationHandler> _logger;
	private readonly IssueStatusChangedNotificationHandler _sut;

	public IssueStatusChangedNotificationHandlerTests()
	{
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<IssueStatusChangedNotificationHandler>>();
		_sut = new IssueStatusChangedNotificationHandler(_mediator, _logger);
	}

	[Fact]
	public async Task Handle_NotifiesWatchers()
	{
		// Arrange
		var notification = new IssueStatusChangedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			IssueTitle = "Feature Request",
			OldStatus = "Open",
			NewStatus = "In Progress",
			IssueOwner = "owner@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert - Currently notifies issue owner (watcher)
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.ToEmail == notification.IssueOwner),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_IncludesOldAndNewStatus()
	{
		// Arrange
		var notification = new IssueStatusChangedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			IssueTitle = "Bug Report",
			OldStatus = "In Progress",
			NewStatus = "Resolved",
			IssueOwner = "reporter@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.Body.Contains(notification.OldStatus) &&
				cmd.Body.Contains(notification.NewStatus)),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_SetsSubjectWithNewStatus()
	{
		// Arrange
		var notification = new IssueStatusChangedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			IssueTitle = "Performance Issue",
			OldStatus = "Open",
			NewStatus = "Closed",
			IssueOwner = "user@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.Subject == $"Status Changed: {notification.IssueTitle} is now {notification.NewStatus}"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WhenMediatorThrows_DoesNotRethrow()
	{
		// Arrange
		var notification = new IssueStatusChangedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			IssueTitle = "Test Issue",
			OldStatus = "New",
			NewStatus = "Assigned",
			IssueOwner = "owner@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<Result>(new InvalidOperationException("Simulated failure")));

		// Act
		var act = () => _sut.Handle(notification, CancellationToken.None);

		// Assert - Handler should catch exception and not rethrow
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task Handle_SendsHtmlEmail()
	{
		// Arrange
		var notification = new IssueStatusChangedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			IssueTitle = "Documentation Update",
			OldStatus = "Draft",
			NewStatus = "Published",
			IssueOwner = "editor@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd => cmd.IsHtml),
			Arg.Any<CancellationToken>());
	}
}
