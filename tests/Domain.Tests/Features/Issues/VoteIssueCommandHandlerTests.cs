// Copyright (c) 2026. All rights reserved.

using Domain.Abstractions;
using Domain.Features.Issues.Commands;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Issues;

/// <summary>
///   Unit tests for the <see cref="VoteIssueCommandHandler" /> class.
/// </summary>
public sealed class VoteIssueCommandHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<VoteIssueCommandHandler> _logger;
	private readonly VoteIssueCommandHandler _handler;

	public VoteIssueCommandHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<VoteIssueCommandHandler>>();
		_handler = new VoteIssueCommandHandler(_issueRepository, _logger);
	}

	[Fact]
	public async Task VoteIssueAsync_ValidRequest_IncrementsVotes()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issue = CreateTestIssue(issueId);
		var userId = "user-123";

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		var command = new VoteIssueCommand(issueId.ToString(), userId);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Votes.Should().Be(1);
	}

	[Fact]
	public async Task VoteIssueAsync_AlreadyVoted_ReturnsValidationError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var userId = "user-123";
		var issue = CreateTestIssue(issueId);
		issue.VotedBy!.Add(userId);
		issue.Votes = 1;

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		var command = new VoteIssueCommand(issueId.ToString(), userId);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("Already voted");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task VoteIssueAsync_IssueNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		_issueRepository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found", ResultErrorCode.NotFound));

		var command = new VoteIssueCommand(issueId, "user-123");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task VoteIssueAsync_FirstVote_AddsUserToVotedBy()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issue = CreateTestIssue(issueId);
		var userId = "user-abc";

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		Issue? capturedIssue = null;
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		var command = new VoteIssueCommand(issueId.ToString(), userId);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue.Should().NotBeNull();
		capturedIssue!.VotedBy.Should().Contain(userId);
	}

	[Fact]
	public async Task VoteIssueAsync_ValidRequest_UpdatesIssueInRepository()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issue = CreateTestIssue(issueId);
		var userId = "user-xyz";

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		var command = new VoteIssueCommand(issueId.ToString(), userId);

		// Act
		await _handler.Handle(command, CancellationToken.None);

		// Assert
		await _issueRepository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task VoteIssueAsync_MultipleUsers_VotesCountMatchesVotedByCount()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issue = CreateTestIssue(issueId);
		issue.VotedBy!.Add("user-existing");
		issue.Votes = 1;

		var newUserId = "user-new";

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		Issue? capturedIssue = null;
		_issueRepository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		var command = new VoteIssueCommand(issueId.ToString(), newUserId);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue!.Votes.Should().Be(capturedIssue.VotedBy!.Count);
		capturedIssue.VotedBy.Should().HaveCount(2);
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
}
