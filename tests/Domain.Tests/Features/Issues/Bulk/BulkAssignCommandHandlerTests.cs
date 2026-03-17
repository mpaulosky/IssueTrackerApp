// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkAssignCommandHandlerTests.cs
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
/// Unit tests for BulkAssignCommandHandler.
/// </summary>
public sealed class BulkAssignCommandHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly INotificationService _notificationService;
	private readonly ILogger<BulkAssignCommandHandler> _logger;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IUndoService _undoService;
	private readonly BulkAssignCommandHandler _sut;

	public BulkAssignCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_notificationService = Substitute.For<INotificationService>();
		_logger = Substitute.For<ILogger<BulkAssignCommandHandler>>();
		_bulkQueue = Substitute.For<IBulkOperationQueue>();
		_undoService = Substitute.For<IUndoService>();
		_sut = new BulkAssignCommandHandler(_repository, _notificationService, _logger, _bulkQueue, _undoService);
	}

	[Fact]
	public async Task BulkAssign_AssignsUserToAllIssues()
	{
		// Arrange
		var issueIds = new List<string> { "issue1", "issue2", "issue3" };
		var assignee = new UserInfo { Id = "user2", Name = "Jane Doe", Email = "jane@example.com" };
		var assigneeDto = new UserDto(assignee);

		var command = new BulkAssignCommand(issueIds, assigneeDto, "user1");

		var issues = issueIds.Select(id => new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = $"Issue {id}",
			Status = StatusInfo.Empty,
			Category = CategoryInfo.Empty,
			Author = new UserInfo { Id = "user1", Name = "Original Author", Email = "original@example.com" },
			DateCreated = DateTime.UtcNow.AddDays(-5)
		}).ToList();

		for (var i = 0; i < issueIds.Count; i++)
		{
			_repository.GetByIdAsync(issueIds[i], Arg.Any<CancellationToken>())
				.Returns(Result.Ok(issues[i]));

			_repository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
				.Returns(Result.Ok(issues[i]));
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

		await _repository.Received(3).UpdateAsync(
			Arg.Is<Issue>(i => i.Author.Id == "user2"),
			Arg.Any<CancellationToken>());

		// Verify notifications were sent
		await _notificationService.Received(3).NotifyIssueAssignedAsync(
			Arg.Any<ObjectId>(),
			Arg.Any<string>(),
			"user2",
			Arg.Any<CancellationToken>());
	}
}
