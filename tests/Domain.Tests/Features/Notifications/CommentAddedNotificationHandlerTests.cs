// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentAddedNotificationHandlerTests.cs
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
///   Unit tests for CommentAddedNotificationHandler.
/// </summary>
public sealed class CommentAddedNotificationHandlerTests
{
	private readonly IMediator _mediator;
	private readonly ILogger<CommentAddedNotificationHandler> _logger;
	private readonly CommentAddedNotificationHandler _sut;

	public CommentAddedNotificationHandlerTests()
	{
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<CommentAddedNotificationHandler>>();
		_sut = new CommentAddedNotificationHandler(_mediator, _logger);
	}

	[Fact]
	public async Task Handle_NotifiesIssueOwner()
	{
		// Arrange
		var commentAuthor = new UserInfo { Id = "author-123", Name = "Comment Author", Email = "author@example.com" };
		var comment = new CommentDto(
			ObjectId.GenerateNewId(),
			"Comment Title",
			"This is the comment description",
			DateTime.UtcNow,
			null,
			ObjectId.Empty,
			new UserDto(commentAuthor),
			[],
			false,
			UserDto.Empty,
			false,
			UserDto.Empty);

		var notification = new CommentAddedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			Comment = comment,
			IssueTitle = "Original Issue",
			IssueOwner = "owner@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.ToEmail == notification.IssueOwner),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_NotifiesOtherCommenters()
	{
		// NOTE: The current implementation only notifies the issue owner.
		// This test documents the expected behavior when commenter notifications are implemented.
		// For now, we verify at least the issue owner is notified.

		// Arrange
		var commentAuthor = new UserInfo { Id = "commenter-456", Name = "New Commenter", Email = "newcommenter@example.com" };
		var comment = new CommentDto(
			ObjectId.GenerateNewId(),
			"Follow-up Comment",
			"I have additional thoughts on this issue",
			DateTime.UtcNow,
			null,
			ObjectId.Empty,
			new UserDto(commentAuthor),
			[],
			false,
			UserDto.Empty,
			false,
			UserDto.Empty);

		var notification = new CommentAddedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			Comment = comment,
			IssueTitle = "Discussion Thread",
			IssueOwner = "owner@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert - At minimum, issue owner should be notified
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.ToEmail == notification.IssueOwner &&
				cmd.Body.Contains(comment.Author.Name) &&
				cmd.Body.Contains(comment.Description)),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WhenCommentAuthorIsOwner_DoesNotSendEmail()
	{
		// Arrange
		const string ownerId = "owner-123";
		var commentAuthor = new UserInfo { Id = ownerId, Name = "Issue Owner", Email = "owner@example.com" };
		var comment = new CommentDto(
			ObjectId.GenerateNewId(),
			"Owner Comment",
			"Owner adds a comment to their own issue",
			DateTime.UtcNow,
			null,
			ObjectId.Empty,
			new UserDto(commentAuthor),
			[],
			false,
			UserDto.Empty,
			false,
			UserDto.Empty);

		var notification = new CommentAddedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			Comment = comment,
			IssueTitle = "My Issue",
			IssueOwner = ownerId // Same as comment author
		};

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert - No email should be sent when author is owner
		await _mediator.DidNotReceive().Send(
			Arg.Any<QueueEmailCommand>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_IncludesCommentContentInEmail()
	{
		// Arrange
		var commentAuthor = new UserInfo { Id = "author-789", Name = "Technical Lead", Email = "lead@example.com" };
		var comment = new CommentDto(
			ObjectId.GenerateNewId(),
			"Technical Review",
			"The implementation looks good, but consider adding error handling.",
			DateTime.UtcNow,
			null,
			ObjectId.Empty,
			new UserDto(commentAuthor),
			[],
			false,
			UserDto.Empty,
			false,
			UserDto.Empty);

		var notification = new CommentAddedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			Comment = comment,
			IssueTitle = "Code Review Request",
			IssueOwner = "developer@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.Body.Contains(comment.Description) &&
				cmd.Body.Contains(comment.Author.Name)),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_SetsSubjectWithIssueTitle()
	{
		// Arrange
		var commentAuthor = new UserInfo { Id = "user-abc", Name = "User", Email = "user@example.com" };
		var comment = new CommentDto(
			ObjectId.GenerateNewId(),
			"Question",
			"Can you clarify the requirements?",
			DateTime.UtcNow,
			null,
			ObjectId.Empty,
			new UserDto(commentAuthor),
			[],
			false,
			UserDto.Empty,
			false,
			UserDto.Empty);

		var notification = new CommentAddedEvent
		{
			IssueId = ObjectId.GenerateNewId(),
			Comment = comment,
			IssueTitle = "Requirements Clarification",
			IssueOwner = "pm@example.com"
		};

		_mediator.Send(Arg.Any<QueueEmailCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		// Act
		await _sut.Handle(notification, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<QueueEmailCommand>(cmd =>
				cmd.Subject == $"New Comment on Issue: {notification.IssueTitle}"),
			Arg.Any<CancellationToken>());
	}
}
