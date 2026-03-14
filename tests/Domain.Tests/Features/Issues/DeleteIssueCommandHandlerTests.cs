// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteIssueCommandHandlerTests.cs
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
///   Unit tests for the <see cref="DeleteIssueCommandHandler" /> class.
/// </summary>
public sealed class DeleteIssueCommandHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<DeleteIssueCommandHandler> _logger;
	private readonly DeleteIssueCommandHandler _handler;

	public DeleteIssueCommandHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<DeleteIssueCommandHandler>>();
		_handler = new DeleteIssueCommandHandler(_issueRepository, _logger);
	}

	[Fact]
	public async Task DeleteIssue_WhenExists_SetsArchivedFlag()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId);
		var archivedBy = new UserInfo { Id = "admin-123", Name = "Admin User", Email = "admin@example.com" };

		var command = new DeleteIssueCommand(issueId.ToString(), new UserDto(archivedBy));

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
		result.Value.Should().BeTrue();

		capturedIssue.Should().NotBeNull();
		capturedIssue!.Archived.Should().BeTrue();
		capturedIssue.ArchivedBy.Should().BeEquivalentTo(archivedBy);

		await _issueRepository.Received(1).GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>());
		await _issueRepository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteIssue_WhenNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		var archivedBy = new UserInfo { Id = "admin-123", Name = "Admin User", Email = "admin@example.com" };

		var command = new DeleteIssueCommand(issueId, new UserDto(archivedBy));

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
	public async Task DeleteIssue_ShouldSetDateModified()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId);
		var beforeTest = DateTime.UtcNow;
		var archivedBy = new UserInfo { Id = "admin-123", Name = "Admin User", Email = "admin@example.com" };

		var command = new DeleteIssueCommand(issueId.ToString(), new UserDto(archivedBy));

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
	public async Task DeleteIssue_WhenRepositoryUpdateFails_ReturnsFailure()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId);
		var archivedBy = new UserInfo { Id = "admin-123", Name = "Admin User", Email = "admin@example.com" };

		var command = new DeleteIssueCommand(issueId.ToString(), new UserDto(archivedBy));

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
	public async Task DeleteIssue_ShouldBeSoftDelete_NotHardDelete()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId);
		var archivedBy = UserInfo.Empty;

		var command = new DeleteIssueCommand(issueId.ToString(), new UserDto(archivedBy));

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();

		// Verify UpdateAsync was called (soft delete) and not DeleteAsync (hard delete)
		await _issueRepository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
		await _issueRepository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	private static Issue CreateTestIssue(ObjectId id)
	{
		return new Issue
		{
			Id = id,
			Title = "Test Issue",
			Description = "Test Description",
			Category = CategoryInfo.Empty,
			Author = UserInfo.Empty,
			Status = StatusInfo.Empty,
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ApprovedForRelease = false,
			Rejected = false
		};
	}
}
