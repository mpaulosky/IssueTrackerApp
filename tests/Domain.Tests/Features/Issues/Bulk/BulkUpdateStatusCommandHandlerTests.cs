// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkUpdateStatusCommandHandlerTests.cs
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
/// Unit tests for BulkUpdateStatusCommandHandler.
/// </summary>
public sealed class BulkUpdateStatusCommandHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly IMediator _mediator;
	private readonly ILogger<BulkUpdateStatusCommandHandler> _logger;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IUndoService _undoService;
	private readonly BulkUpdateStatusCommandHandler _sut;

	public BulkUpdateStatusCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<BulkUpdateStatusCommandHandler>>();
		_bulkQueue = Substitute.For<IBulkOperationQueue>();
		_undoService = Substitute.For<IUndoService>();
		_sut = new BulkUpdateStatusCommandHandler(_repository, _mediator, _logger, _bulkQueue, _undoService);
	}

	[Fact]
	public async Task BulkUpdateStatus_UpdatesAllSelectedIssues()
	{
		// Arrange
		var issueIds = new List<string> { "issue1", "issue2", "issue3" };
		var newStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"Closed",
			"Closed status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var command = new BulkUpdateStatusCommand(issueIds, newStatus, "user1");

		foreach (var issueId in issueIds)
		{
			var issue = new Issue
			{
				Id = ObjectId.Parse(ObjectId.GenerateNewId().ToString()),
				Title = $"Issue {issueId}",
				Status = StatusDto.Empty,
				Category = CategoryDto.Empty,
				Author = UserDto.Empty,
				DateCreated = DateTime.UtcNow.AddDays(-5)
			};

			_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
				.Returns(Result.Ok(issue));

			_repository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
				.Returns(Result.Ok(issue));
		}

		_undoService.StoreUndoDataAsync(
				Arg.Any<string>(),
				Arg.Any<List<IssueUndoSnapshot>>(),
				Arg.Any<CancellationToken>())
			.Returns("undo-token-123");

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.SuccessCount.Should().Be(3);
		result.Value!.FailureCount.Should().Be(0);

		await _repository.Received(3).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task BulkUpdateStatus_ReturnsSuccessCount()
	{
		// Arrange
		var issueIds = new List<string> { "issue1", "issue2" };
		var newStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"In Progress",
			"In progress status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var command = new BulkUpdateStatusCommand(issueIds, newStatus, "user1");

		// Setup first issue to succeed
		var issue1 = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Issue 1",
			Status = StatusDto.Empty,
			Category = CategoryDto.Empty,
			Author = UserDto.Empty
		};

		_repository.GetByIdAsync("issue1", Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue1));

		_repository.UpdateAsync(Arg.Is<Issue>(i => i.Id == issue1.Id), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue1));

		// Setup second issue to fail (not found)
		_repository.GetByIdAsync("issue2", Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found"));

		_undoService.StoreUndoDataAsync(
				Arg.Any<string>(),
				Arg.Any<List<IssueUndoSnapshot>>(),
				Arg.Any<CancellationToken>())
			.Returns("undo-token-123");

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.SuccessCount.Should().Be(1);
		result.Value!.FailureCount.Should().Be(1);
		result.Value!.Errors.Should().HaveCount(1);
		result.Value!.Errors.First().IssueId.Should().Be("issue2");
	}

	[Fact]
	public async Task BulkUpdateStatus_QueuesForBackgroundWhenOverThreshold()
	{
		// Arrange
		var issueIds = Enumerable.Range(1, BulkOperationConstants.BackgroundThreshold + 10)
			.Select(i => $"issue{i}")
			.ToList();

		var newStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"Closed",
			"Closed status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var command = new BulkUpdateStatusCommand(issueIds, newStatus, "user1");

		_bulkQueue.QueueAsync(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns("operation-id-123");

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsQueued.Should().BeTrue();
		result.Value!.OperationId.Should().Be("operation-id-123");

		await _bulkQueue.Received(1).QueueAsync(
			Arg.Any<BulkUpdateStatusCommand>(),
			Arg.Any<CancellationToken>());

		// Repository should not be called when queued
		await _repository.DidNotReceive().GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}
}
