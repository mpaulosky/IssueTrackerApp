// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkDeleteCommandHandlerTests.cs
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
/// Unit tests for BulkDeleteCommandHandler.
/// </summary>
public sealed class BulkDeleteCommandHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<BulkDeleteCommandHandler> _logger;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IUndoService _undoService;
	private readonly BulkDeleteCommandHandler _sut;

	public BulkDeleteCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<BulkDeleteCommandHandler>>();
		_bulkQueue = Substitute.For<IBulkOperationQueue>();
		_undoService = Substitute.For<IUndoService>();
		_sut = new BulkDeleteCommandHandler(_repository, _logger, _bulkQueue, _undoService);
	}

	[Fact]
	public async Task BulkDelete_ArchivesAllSelectedIssues()
	{
		// Arrange
		var issueIds = new List<string> { "issue1", "issue2", "issue3" };
		var deletedBy = new UserInfo { Id = "admin1", Name = "Admin User", Email = "admin@example.com" };
		var deletedByDto = new UserDto(deletedBy);

		var command = new BulkDeleteCommand(issueIds, deletedByDto, "admin1");

		var issues = issueIds.Select(id => new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = $"Issue {id}",
			Status = StatusInfo.Empty,
			Category = CategoryInfo.Empty,
			Author = UserInfo.Empty,
			Archived = false,
			ArchivedBy = UserInfo.Empty,
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

		// Verify soft delete (archive) was applied
		await _repository.Received(3).UpdateAsync(
			Arg.Is<Issue>(i => i.Archived == true && i.ArchivedBy.Id == "admin1"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task BulkDelete_SkipsAlreadyArchivedIssues()
	{
		// Arrange
		var issueIds = new List<string> { "issue1", "issue2" };
		var deletedBy = new UserInfo { Id = "admin1", Name = "Admin User", Email = "admin@example.com" };
		var deletedByDto = new UserDto(deletedBy);

		var command = new BulkDeleteCommand(issueIds, deletedByDto, "admin1");

		// First issue is already archived
		var alreadyArchivedIssue = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Already Archived Issue",
			Status = StatusInfo.Empty,
			Category = CategoryInfo.Empty,
			Author = UserInfo.Empty,
			Archived = true,
			ArchivedBy = new UserInfo { Id = "other-admin", Name = "Other Admin", Email = "other@example.com" },
			DateCreated = DateTime.UtcNow.AddDays(-10)
		};

		// Second issue is not archived
		var activeIssue = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Active Issue",
			Status = StatusInfo.Empty,
			Category = CategoryInfo.Empty,
			Author = UserInfo.Empty,
			Archived = false,
			ArchivedBy = UserInfo.Empty,
			DateCreated = DateTime.UtcNow.AddDays(-5)
		};

		_repository.GetByIdAsync("issue1", Arg.Any<CancellationToken>())
			.Returns(Result.Ok(alreadyArchivedIssue));

		_repository.GetByIdAsync("issue2", Arg.Any<CancellationToken>())
			.Returns(Result.Ok(activeIssue));

		_repository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(activeIssue));

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
		result.Value!.Errors.Should().Contain(e => e.ErrorMessage.Contains("already archived"));
	}
}
