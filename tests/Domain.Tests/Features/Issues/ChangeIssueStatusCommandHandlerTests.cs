// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ChangeIssueStatusCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;
using Domain.Events;
using Domain.Features.Issues.Commands;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Issues;

/// <summary>
///   Unit tests for the <see cref="ChangeIssueStatusCommandHandler" /> class.
/// </summary>
public sealed class ChangeIssueStatusCommandHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly IMediator _mediator;
	private readonly ILogger<ChangeIssueStatusCommandHandler> _logger;
	private readonly ChangeIssueStatusCommandHandler _handler;

	public ChangeIssueStatusCommandHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<ChangeIssueStatusCommandHandler>>();
		_handler = new ChangeIssueStatusCommandHandler(_issueRepository, _mediator, _logger);
	}

	[Fact]
	public async Task ChangeStatus_WhenValid_UpdatesStatus()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId, "Open");

		var newStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "In Progress",
			StatusDescription = "Issue is being worked on",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new ChangeIssueStatusCommand(issueId.ToString(), StatusMapper.ToDto(newStatus));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		Issue? capturedIssue = null;
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Status.Should().Be(newStatus);

		capturedIssue.Should().NotBeNull();
		capturedIssue!.Status.StatusName.Should().Be("In Progress");

		await _issueRepository.Received(1).GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>());
		await _issueRepository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ChangeStatus_PublishesIssueStatusChangedEvent()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var author = new UserInfo { Id = "user-123", Name = "John Doe", Email = "john@example.com" };
		var existingIssue = CreateTestIssue(issueId, "Open", author: author);

		var newStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Closed",
			StatusDescription = "Issue is closed",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new ChangeIssueStatusCommand(issueId.ToString(), StatusMapper.ToDto(newStatus));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		IssueStatusChangedEvent? capturedEvent = null;
		await _mediator.Publish(Arg.Do<IssueStatusChangedEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>());

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();

		await _mediator.Received(1).Publish(Arg.Any<IssueStatusChangedEvent>(), Arg.Any<CancellationToken>());

		capturedEvent.Should().NotBeNull();
		capturedEvent!.IssueId.Should().Be(issueId);
		capturedEvent.IssueTitle.Should().Be(existingIssue.Title);
		capturedEvent.OldStatus.Should().Be("Open");
		capturedEvent.NewStatus.Should().Be("Closed");
		capturedEvent.IssueOwner.Should().Be(author.Id);
	}

	[Fact]
	public async Task ChangeStatus_WhenIssueNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();

		var newStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Closed",
			StatusDescription = "Issue is closed",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new ChangeIssueStatusCommand(issueId, StatusMapper.ToDto(newStatus));

		_issueRepository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found", ResultErrorCode.NotFound));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Contain("not found");

		await _issueRepository.Received(1).GetByIdAsync(issueId, Arg.Any<CancellationToken>());
		await _issueRepository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
		await _mediator.DidNotReceive().Publish(Arg.Any<IssueStatusChangedEvent>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ChangeStatus_ShouldUpdateDateModified()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId, "Open");
		var beforeTest = DateTime.UtcNow;

		var newStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "In Progress",
			StatusDescription = "Issue is being worked on",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new ChangeIssueStatusCommand(issueId.ToString(), StatusMapper.ToDto(newStatus));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		Issue? capturedIssue = null;
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		var afterTest = DateTime.UtcNow;

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue.Should().NotBeNull();
		capturedIssue!.DateModified.Should().NotBeNull();
		capturedIssue.DateModified!.Value.Should().BeOnOrAfter(beforeTest);
		capturedIssue.DateModified!.Value.Should().BeOnOrBefore(afterTest);
	}

	[Fact]
	public async Task ChangeStatus_WhenRepositoryUpdateFails_ReturnsFailureAndDoesNotPublishEvent()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId, "Open");

		var newStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "In Progress",
			StatusDescription = "Issue is being worked on",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new ChangeIssueStatusCommand(issueId.ToString(), StatusMapper.ToDto(newStatus));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Database error", ResultErrorCode.Conflict));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("error");

		await _mediator.DidNotReceive().Publish(Arg.Any<IssueStatusChangedEvent>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ChangeStatus_FromClosedToOpen_WorksCorrectly()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId, "Closed");

		var newStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Open",
			StatusDescription = "Issue is reopened",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new ChangeIssueStatusCommand(issueId.ToString(), StatusMapper.ToDto(newStatus));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Status.StatusName.Should().Be("Open");

		await _mediator.Received(1).Publish(
			Arg.Is<IssueStatusChangedEvent>(e =>
				e.OldStatus == "Closed" &&
				e.NewStatus == "Open"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ChangeStatus_EventContainsCorrectTimestamp()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId, "Open");
		var beforeTest = DateTime.UtcNow;

		var newStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Closed",
			StatusDescription = "Issue is closed",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new ChangeIssueStatusCommand(issueId.ToString(), StatusMapper.ToDto(newStatus));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		IssueStatusChangedEvent? capturedEvent = null;
		await _mediator.Publish(Arg.Do<IssueStatusChangedEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>());

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		var afterTest = DateTime.UtcNow;

		// Assert
		result.Success.Should().BeTrue();
		capturedEvent.Should().NotBeNull();
		capturedEvent!.Timestamp.Should().BeOnOrAfter(beforeTest);
		capturedEvent.Timestamp.Should().BeOnOrBefore(afterTest);
	}

	private static Issue CreateTestIssue(
		ObjectId id,
		string statusName,
		UserInfo? author = null)
	{
		var status = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = statusName,
			StatusDescription = $"{statusName} status",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		return new Issue
		{
			Id = id,
			Title = "Test Issue",
			Description = "Test Description",
			Category = CategoryInfo.Empty,
			Author = author ?? UserInfo.Empty,
			Status = status,
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ApprovedForRelease = false,
			Rejected = false
		};
	}
}
