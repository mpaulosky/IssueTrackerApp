// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     QueueEmailCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;
using Domain.Features.Notifications;

using Microsoft.Extensions.Logging;

namespace Domain.Tests.Features.Notifications;

/// <summary>
///   Unit tests for QueueEmailCommandHandler.
/// </summary>
public sealed class QueueEmailCommandHandlerTests
{
	private readonly IRepository<EmailQueueItem> _emailQueueRepository;
	private readonly ILogger<QueueEmailCommandHandler> _logger;
	private readonly QueueEmailCommandHandler _sut;

	public QueueEmailCommandHandlerTests()
	{
		_emailQueueRepository = Substitute.For<IRepository<EmailQueueItem>>();
		_logger = Substitute.For<ILogger<QueueEmailCommandHandler>>();
		_sut = new QueueEmailCommandHandler(_emailQueueRepository, _logger);
	}

	[Fact]
	public async Task QueueEmail_AddsToQueue()
	{
		// Arrange
		var command = new QueueEmailCommand
		{
			ToEmail = "recipient@example.com",
			Subject = "Test Subject",
			Body = "<html><body>Test Body</body></html>",
			IsHtml = true
		};

		EmailQueueItem? capturedItem = null;
		_emailQueueRepository.AddAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedItem = callInfo.Arg<EmailQueueItem>();
				return Result.Ok(capturedItem);
			});

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _emailQueueRepository.Received(1).AddAsync(
			Arg.Is<EmailQueueItem>(item =>
				item.ToEmail == command.ToEmail &&
				item.Subject == command.Subject &&
				item.Body == command.Body &&
				item.IsHtml == command.IsHtml),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task QueueEmail_SetsCorrectPriority()
	{
		// Arrange
		var command = new QueueEmailCommand
		{
			ToEmail = "user@example.com",
			Subject = "Priority Email",
			Body = "Important message"
		};

		EmailQueueItem? capturedItem = null;
		_emailQueueRepository.AddAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedItem = callInfo.Arg<EmailQueueItem>();
				return Result.Ok(capturedItem);
			});

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedItem.Should().NotBeNull();
		capturedItem!.Status.Should().Be(EmailQueueStatus.Pending);
		capturedItem.NextAttemptAt.Should().NotBeNull();
		capturedItem.NextAttemptAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task QueueEmail_SetsQueuedAtTimestamp()
	{
		// Arrange
		var beforeTest = DateTime.UtcNow;
		var command = new QueueEmailCommand
		{
			ToEmail = "recipient@example.com",
			Subject = "Timestamp Test",
			Body = "Body content"
		};

		EmailQueueItem? capturedItem = null;
		_emailQueueRepository.AddAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedItem = callInfo.Arg<EmailQueueItem>();
				return Result.Ok(capturedItem);
			});

		// Act
		await _sut.Handle(command, CancellationToken.None);
		var afterTest = DateTime.UtcNow;

		// Assert
		capturedItem.Should().NotBeNull();
		capturedItem!.QueuedAt.Should().BeOnOrAfter(beforeTest);
		capturedItem.QueuedAt.Should().BeOnOrBefore(afterTest);
	}

	[Fact]
	public async Task QueueEmail_WhenRepositoryThrows_ReturnsFailure()
	{
		// Arrange
		var command = new QueueEmailCommand
		{
			ToEmail = "user@example.com",
			Subject = "Failure Test",
			Body = "This should fail"
		};

		_emailQueueRepository.AddAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<Result<EmailQueueItem>>(
				new InvalidOperationException("Database connection failed")));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to queue email");
	}

	[Fact]
	public async Task QueueEmail_WithFromName_SetsFromName()
	{
		// Arrange
		var command = new QueueEmailCommand
		{
			ToEmail = "recipient@example.com",
			Subject = "From Name Test",
			Body = "Body content",
			FromName = "Custom Sender"
		};

		EmailQueueItem? capturedItem = null;
		_emailQueueRepository.AddAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedItem = callInfo.Arg<EmailQueueItem>();
				return Result.Ok(capturedItem);
			});

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		capturedItem.Should().NotBeNull();
		capturedItem!.FromName.Should().Be("Custom Sender");
	}

	[Fact]
	public async Task QueueEmail_WithPlainText_SetsIsHtmlFalse()
	{
		// Arrange
		var command = new QueueEmailCommand
		{
			ToEmail = "recipient@example.com",
			Subject = "Plain Text Email",
			Body = "This is plain text without HTML",
			IsHtml = false
		};

		EmailQueueItem? capturedItem = null;
		_emailQueueRepository.AddAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedItem = callInfo.Arg<EmailQueueItem>();
				return Result.Ok(capturedItem);
			});

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		capturedItem.Should().NotBeNull();
		capturedItem!.IsHtml.Should().BeFalse();
	}
}
