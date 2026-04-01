// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddLabelCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Issues.Commands;

namespace Domain.Tests.Features.Issues.Commands;

/// <summary>
///   Unit tests for AddLabelCommandHandler.
/// </summary>
public sealed class AddLabelCommandHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly IMediator _mediator;
	private readonly AddLabelCommandHandler _handler;

	public AddLabelCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_mediator = Substitute.For<IMediator>();
		_handler = new AddLabelCommandHandler(
			_repository,
			_mediator,
			new NullLogger<AddLabelCommandHandler>());
	}

	[Fact]
	public async Task Handle_WithValidLabelAndIssueWithRoomForMore_AddsLabel()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		var issue = new Issue
		{
			Id = ObjectId.Parse(issueId),
			Title = "Test Issue",
			Labels = ["alpha", "beta", "gamma"]
		};

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		_repository.UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<Issue>()));

		var command = new AddLabelCommand(issueId, "delta");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Labels.Should().Contain("delta");
		await _repository.Received(1).UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WithDuplicateLabel_ReturnsSuccessWithoutChange()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		var issue = new Issue
		{
			Id = ObjectId.Parse(issueId),
			Title = "Test Issue",
			Labels = ["bug", "feature"]
		};

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		var command = new AddLabelCommand(issueId, "bug");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		await _repository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WhenIssueHasTenLabels_ReturnsValidationError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		var issue = new Issue
		{
			Id = ObjectId.Parse(issueId),
			Title = "Test Issue",
			Labels = ["l1", "l2", "l3", "l4", "l5", "l6", "l7", "l8", "l9", "l10"]
		};

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		var command = new AddLabelCommand(issueId, "l11");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await _repository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WhenIssueNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();

		_repository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found", ResultErrorCode.NotFound));

		var command = new AddLabelCommand(issueId, "bug");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		await _repository.DidNotReceive().UpdateAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}
}
