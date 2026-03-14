// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateIssueCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;
using Domain.Features.Issues.Commands;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Issues;

/// <summary>
///   Unit tests for the <see cref="UpdateIssueCommandHandler" /> class.
/// </summary>
public sealed class UpdateIssueCommandHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<UpdateIssueCommandHandler> _logger;
	private readonly UpdateIssueCommandHandler _handler;

	public UpdateIssueCommandHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<UpdateIssueCommandHandler>>();
		_handler = new UpdateIssueCommandHandler(_issueRepository, _logger);
	}

	[Fact]
	public async Task UpdateIssue_WhenIssueExists_ReturnsSuccess()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId);

		var newCategory = new CategoryInfo
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Updated Category",
			CategoryDescription = "Updated Description",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new UpdateIssueCommand(
			issueId.ToString(),
			"Updated Title",
			"Updated Description",
			CategoryMapper.ToDto(newCategory));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var issue = callInfo.Arg<Issue>();
				return Result.Ok(issue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().Be("Updated Title");
		result.Value.Description.Should().Be("Updated Description");
		result.Value.Category.Should().Be(newCategory);

		await _issueRepository.Received(1).GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>());
		await _issueRepository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdateIssue_WhenIssueNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();

		var command = new UpdateIssueCommand(
			issueId,
			"Updated Title",
			"Updated Description",
			CategoryMapper.ToDto(CategoryInfo.Empty));

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
	}

	[Fact]
	public async Task UpdateIssue_ShouldUpdateModifiedAt()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId);
		var beforeTest = DateTime.UtcNow;

		var command = new UpdateIssueCommand(
			issueId.ToString(),
			"Updated Title",
			"Updated Description",
			CategoryMapper.ToDto(CategoryInfo.Empty));

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
	public async Task UpdateIssue_WhenRepositoryUpdateFails_ReturnsFailure()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId);

		var command = new UpdateIssueCommand(
			issueId.ToString(),
			"Updated Title",
			"Updated Description",
			CategoryMapper.ToDto(CategoryInfo.Empty));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Database error", ResultErrorCode.Conflict));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("error");
	}

	[Fact]
	public async Task UpdateIssue_ShouldPreserveOriginalAuthor()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var originalAuthor = new UserInfo { Id = "original-author", Name = "Original Author", Email = "original@example.com" };
		var existingIssue = CreateTestIssue(issueId, author: originalAuthor);

		var command = new UpdateIssueCommand(
			issueId.ToString(),
			"Updated Title",
			"Updated Description",
			CategoryMapper.ToDto(CategoryInfo.Empty));

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
		capturedIssue.Should().NotBeNull();
		capturedIssue!.Author.Should().Be(originalAuthor);
	}

	private static Issue CreateTestIssue(
		ObjectId id,
		string title = "Test Issue",
		string description = "Test Description",
		UserInfo? author = null)
	{
		return new Issue
		{
			Id = id,
			Title = title,
			Description = description,
			Category = CategoryInfo.Empty,
			Author = author ?? UserInfo.Empty,
			Status = StatusInfo.Empty,
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ApprovedForRelease = false,
			Rejected = false
		};
	}
}
