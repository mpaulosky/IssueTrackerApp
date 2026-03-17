// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UndoBulkOperationCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;
using Domain.Features.Issues.Commands.Bulk;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Issues.Bulk;

/// <summary>
/// Unit tests for UndoBulkOperationCommandHandler.
/// </summary>
public sealed class UndoBulkOperationCommandHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly IUndoService _undoService;
	private readonly ILogger<UndoBulkOperationCommandHandler> _logger;
	private readonly UndoBulkOperationCommandHandler _sut;

	public UndoBulkOperationCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_undoService = Substitute.For<IUndoService>();
		_logger = Substitute.For<ILogger<UndoBulkOperationCommandHandler>>();
		_sut = new UndoBulkOperationCommandHandler(_repository, _undoService, _logger);
	}

	[Fact]
	public async Task Undo_RestoresPreviousState()
	{
		// Arrange
		var undoToken = "undo-token-123";
		var requestedBy = "user1";
		var command = new UndoBulkOperationCommand(undoToken, requestedBy);

		var previousStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Open",
			StatusDescription = "Open status",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var currentStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Closed",
			StatusDescription = "Closed status",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var issueId = "issue-123";
		var issue = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Test Issue",
			Status = currentStatus, // Current state is "Closed"
			Category = CategoryInfo.Empty,
			Author = UserInfo.Empty,
			DateCreated = DateTime.UtcNow.AddDays(-5)
		};

		var snapshots = new List<IssueUndoSnapshot>
		{
			new(issueId, BulkOperationType.StatusUpdate, new StatusUpdateSnapshot(StatusMapper.ToDto(previousStatus)))
		};

		var undoData = new UndoData(requestedBy, snapshots, DateTime.UtcNow.AddMinutes(-5));

		_undoService.GetUndoDataAsync(undoToken, requestedBy, Arg.Any<CancellationToken>())
			.Returns(undoData);

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		_repository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.SuccessCount.Should().Be(1);
		result.Value!.FailureCount.Should().Be(0);

		await _repository.Received(1).UpdateAsync(
			Arg.Is<Issue>(i => i.Status.StatusName == "Open"),
			Arg.Any<CancellationToken>());

		await _undoService.Received(1).InvalidateUndoTokenAsync(undoToken, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Undo_WhenExpired_ReturnsError()
	{
		// Arrange
		var undoToken = "expired-token";
		var requestedBy = "user1";
		var command = new UndoBulkOperationCommand(undoToken, requestedBy);

		// Return null to simulate expired/not found token
		_undoService.GetUndoDataAsync(undoToken, requestedBy, Arg.Any<CancellationToken>())
			.Returns((UndoData?)null);

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");

		await _repository.DidNotReceive().GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
		await _repository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Undo_WhenWrongUser_ReturnsError()
	{
		// Arrange
		var undoToken = "token-for-different-user";
		var requestedBy = "different-user";
		var command = new UndoBulkOperationCommand(undoToken, requestedBy);

		// Return null to simulate token belonging to different user
		_undoService.GetUndoDataAsync(undoToken, requestedBy, Arg.Any<CancellationToken>())
			.Returns((UndoData?)null);

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task Undo_RestoresDeletedIssue()
	{
		// Arrange
		var undoToken = "undo-delete-token";
		var requestedBy = "admin1";
		var command = new UndoBulkOperationCommand(undoToken, requestedBy);

		var issueId = "deleted-issue-123";
		var archivedBy = new UserInfo { Id = "admin1", Name = "Admin", Email = "admin@example.com" };
		var issue = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Archived Issue",
			Status = StatusInfo.Empty,
			Category = CategoryInfo.Empty,
			Author = UserInfo.Empty,
			Archived = true,
			ArchivedBy = archivedBy,
			DateCreated = DateTime.UtcNow.AddDays(-10)
		};

		var snapshots = new List<IssueUndoSnapshot>
		{
			new(issueId, BulkOperationType.Delete, new DeleteSnapshot(false, UserDto.Empty))
		};

		var undoData = new UndoData(requestedBy, snapshots, DateTime.UtcNow.AddMinutes(-5));

		_undoService.GetUndoDataAsync(undoToken, requestedBy, Arg.Any<CancellationToken>())
			.Returns(undoData);

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		_repository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.SuccessCount.Should().Be(1);

		await _repository.Received(1).UpdateAsync(
			Arg.Is<Issue>(i => i.Archived == false),
			Arg.Any<CancellationToken>());
	}
}
