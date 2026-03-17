// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkUpdateCategoryCommandHandlerTests.cs
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
/// Unit tests for BulkUpdateCategoryCommandHandler.
/// </summary>
public sealed class BulkUpdateCategoryCommandHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<BulkUpdateCategoryCommandHandler> _logger;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IUndoService _undoService;
	private readonly BulkUpdateCategoryCommandHandler _sut;

	public BulkUpdateCategoryCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<BulkUpdateCategoryCommandHandler>>();
		_bulkQueue = Substitute.For<IBulkOperationQueue>();
		_undoService = Substitute.For<IUndoService>();
		_sut = new BulkUpdateCategoryCommandHandler(_repository, _logger, _bulkQueue, _undoService);
	}

	[Fact]
	public async Task BulkUpdateCategory_UpdatesAllSelectedIssues()
	{
		// Arrange
		var issueIds = new List<string> { "issue1", "issue2", "issue3" };
		var newCategory = new CategoryInfo
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Feature",
			CategoryDescription = "Feature requests",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var newCategoryDto = CategoryMapper.ToDto(newCategory);

		var command = new BulkUpdateCategoryCommand(issueIds, newCategoryDto, "user1");

		foreach (var issueId in issueIds)
		{
			var issue = new Issue
			{
				Id = ObjectId.GenerateNewId(),
				Title = $"Issue {issueId}",
				Status = StatusInfo.Empty,
				Category = CategoryInfo.Empty,
				Author = UserInfo.Empty,
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
		result.Value!.TotalRequested.Should().Be(3);

		await _repository.Received(3).UpdateAsync(
			Arg.Is<Issue>(i => i.Category.CategoryName == "Feature"),
			Arg.Any<CancellationToken>());
	}
}
