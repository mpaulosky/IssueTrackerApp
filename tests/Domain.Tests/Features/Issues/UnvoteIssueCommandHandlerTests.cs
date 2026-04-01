// Copyright (c) 2026. All rights reserved.

using Domain.Abstractions;
using Domain.Features.Issues.Commands;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Issues;

/// <summary>
///   Unit tests for the <see cref="UnvoteIssueCommandHandler" /> class.
/// </summary>
public sealed class UnvoteIssueCommandHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<UnvoteIssueCommandHandler> _logger;
	private readonly UnvoteIssueCommandHandler _handler;

	public UnvoteIssueCommandHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<UnvoteIssueCommandHandler>>();
		_handler = new UnvoteIssueCommandHandler(_issueRepository, _logger);
	}

	[Fact]
	public async Task UnvoteIssueAsync_ValidRequest_DecrementsVotes()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var userId = "user-123";
		var issue = CreateTestIssueWithVote(issueId, userId);

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		var command = new UnvoteIssueCommand(issueId.ToString(), userId);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Votes.Should().Be(0);
	}

	[Fact]
	public async Task UnvoteIssueAsync_NotVoted_ReturnsValidationError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var userId = "user-who-never-voted";
		var issue = CreateTestIssue(issueId);

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		var command = new UnvoteIssueCommand(issueId.ToString(), userId);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("Not voted");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task UnvoteIssueAsync_ValidRequest_RemovesUserFromVotedBy()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var userId = "user-123";
		var issue = CreateTestIssueWithVote(issueId, userId);

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		Issue? capturedIssue = null;
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		var command = new UnvoteIssueCommand(issueId.ToString(), userId);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue.Should().NotBeNull();
		capturedIssue!.VotedBy.Should().NotContain(userId);
	}

	[Fact]
	public async Task UnvoteIssueAsync_IssueNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		_issueRepository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found", ResultErrorCode.NotFound));

		var command = new UnvoteIssueCommand(issueId, "user-123");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task UnvoteIssueAsync_ValidRequest_UpdatesIssueInRepository()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var userId = "user-999";
		var issue = CreateTestIssueWithVote(issueId, userId);

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		var command = new UnvoteIssueCommand(issueId.ToString(), userId);

		// Act
		await _handler.Handle(command, CancellationToken.None);

		// Assert
		await _issueRepository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UnvoteIssueAsync_WithMultipleVoters_OnlyRemovesTargetUser()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var userId = "user-123";
		var otherUserId = "user-456";
		var issue = CreateTestIssue(issueId);
		issue.VotedBy.Add(userId);
		issue.VotedBy.Add(otherUserId);
		issue.Votes = 2;

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		Issue? capturedIssue = null;
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		var command = new UnvoteIssueCommand(issueId.ToString(), userId);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue!.VotedBy.Should().NotContain(userId);
		capturedIssue.VotedBy.Should().Contain(otherUserId);
		capturedIssue.Votes.Should().Be(1);
	}

	private static Issue CreateTestIssue(ObjectId id)
	{
		return new Issue
		{
			Id = id,
			Title = "Test Issue",
			Description = "Test Description",
			Votes = 0,
			VotedBy = []
		};
	}

	private static Issue CreateTestIssueWithVote(ObjectId id, string userId)
	{
		return new Issue
		{
			Id = id,
			Title = "Test Issue",
			Description = "Test Description",
			Votes = 1,
			VotedBy = [userId]
		};
	}
}
