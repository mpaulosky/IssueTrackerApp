// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RemoveLabelCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Issues.Commands;

namespace Domain.Tests.Features.Issues.Commands;

/// <summary>
///   Unit tests for RemoveLabelCommandHandler.
/// </summary>
public sealed class RemoveLabelCommandHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly IMediator _mediator;
	private readonly RemoveLabelCommandHandler _handler;

	public RemoveLabelCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_mediator = Substitute.For<IMediator>();
		_handler = new RemoveLabelCommandHandler(
			_repository,
			_mediator,
			new NullLogger<RemoveLabelCommandHandler>());
	}

	[Fact]
	public async Task Handle_WithExistingLabel_RemovesLabel()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		var issue = new Issue
		{
			Id = ObjectId.Parse(issueId),
			Title = "Test Issue",
			Labels = ["bug", "feature", "urgent"]
		};

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		_repository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		var command = new RemoveLabelCommand(issueId, "feature");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Labels.Should().NotContain("feature");
		await _repository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WithAbsentLabel_ReturnsSuccessWithoutChange()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		var issue = new Issue
		{
			Id = ObjectId.Parse(issueId),
			Title = "Test Issue",
			Labels = ["bug"]
		};

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		var command = new RemoveLabelCommand(issueId, "nonexistent");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		await _repository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WhenIssueNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found", ResultErrorCode.NotFound));

		var command = new RemoveLabelCommand(issueId, "bug");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		await _repository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}
}
