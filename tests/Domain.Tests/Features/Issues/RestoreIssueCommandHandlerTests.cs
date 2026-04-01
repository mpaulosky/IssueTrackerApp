// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RestoreIssueCommandHandlerTests.cs
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
///   Unit tests for the <see cref="RestoreIssueCommandHandler" /> class.
/// </summary>
public sealed class RestoreIssueCommandHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<RestoreIssueCommandHandler> _logger;
	private readonly RestoreIssueCommandHandler _handler;

	public RestoreIssueCommandHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<RestoreIssueCommandHandler>>();
		_handler = new RestoreIssueCommandHandler(_issueRepository, _logger);
	}

	[Fact]
	public async Task RestoreIssue_WhenArchived_ClearsArchivedFlag()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var archivedIssue = CreateArchivedIssue(issueId);

		var command = new RestoreIssueCommand(issueId.ToString());

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archivedIssue));

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
		capturedIssue!.Archived.Should().BeFalse();
		capturedIssue.ArchivedBy.Should().BeEquivalentTo(UserInfo.Empty);

		await _issueRepository.Received(1).GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>());
		await _issueRepository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task RestoreIssue_SetsDateModified()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var archivedIssue = CreateArchivedIssue(issueId);
		var beforeTest = DateTime.UtcNow;

		var command = new RestoreIssueCommand(issueId.ToString());

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archivedIssue));

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
		capturedIssue!.DateModified.Should().NotBeNull();
		capturedIssue.DateModified!.Value.Should().BeOnOrAfter(beforeTest);
		capturedIssue.DateModified.Value.Should().BeOnOrBefore(afterTest);
	}

	[Fact]
	public async Task RestoreIssue_WhenNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		var command = new RestoreIssueCommand(issueId);

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
	public async Task RestoreIssue_WhenNotArchived_ReturnsValidationError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var activeIssue = CreateActiveIssue(issueId);
		var command = new RestoreIssueCommand(issueId.ToString());

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(activeIssue));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Contain("not archived");

		await _issueRepository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task RestoreIssue_WhenRepositoryUpdateFails_ReturnsFailure()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var archivedIssue = CreateArchivedIssue(issueId);
		var command = new RestoreIssueCommand(issueId.ToString());

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archivedIssue));

		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Database error", ResultErrorCode.Conflict));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("error");
	}

	private static Issue CreateArchivedIssue(ObjectId id)
	{
		return new Issue
		{
			Id = id,
			Title = "Archived Issue",
			Description = "This issue is archived",
			Category = CategoryInfo.Empty,
			Author = new UserInfo { Id = "user-1", Name = "User One", Email = "user@example.com" },
			Status = StatusInfo.Empty,
			DateCreated = DateTime.UtcNow.AddDays(-5),
			Archived = true,
			ArchivedBy = new UserInfo { Id = "admin-1", Name = "Admin", Email = "admin@example.com" },
			ApprovedForRelease = false,
			Rejected = false
		};
	}

	private static Issue CreateActiveIssue(ObjectId id)
	{
		return new Issue
		{
			Id = id,
			Title = "Active Issue",
			Description = "This issue is active",
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
