// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddCommentCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Comments;

/// <summary>
///   Unit tests for AddCommentCommandHandler.
/// </summary>
public sealed class AddCommentCommandHandlerTests
{
	private readonly IRepository<Comment> _commentRepository;
	private readonly IRepository<Issue> _issueRepository;
	private readonly IMediator _mediator;
	private readonly ILogger<AddCommentCommandHandler> _logger;
	private readonly AddCommentCommandHandler _sut;

	public AddCommentCommandHandlerTests()
	{
		_commentRepository = Substitute.For<IRepository<Comment>>();
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<AddCommentCommandHandler>>();
		_sut = new AddCommentCommandHandler(
			_commentRepository,
			_issueRepository,
			_mediator,
			_logger);
	}

	[Fact]
	public async Task AddComment_WithValidData_ReturnsSuccess()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var author = new UserInfo { Id = "user-123", Name = "Test User", Email = "test@example.com" };
		var authorDto = new UserDto(author);
		var issue = new Issue
		{
			Id = issueId,
			Title = "Test Issue",
			Description = "Test Description",
			Author = author
		};

		var command = new AddCommentCommand(
			issueId.ToString(),
			"Comment Title",
			"Comment Description",
			authorDto);

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		_commentRepository.AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var comment = callInfo.Arg<Comment>();
				return Result.Ok(comment);
			});

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().Be("Comment Title");
		result.Value.Description.Should().Be("Comment Description");
		result.Value.Author.Should().Be(author);
	}

	[Fact]
	public async Task AddComment_PublishesCommentAddedEvent()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var author = new UserInfo { Id = "user-123", Name = "Test User", Email = "test@example.com" };
		var authorDto = new UserDto(author);
		var issue = new Issue
		{
			Id = issueId,
			Title = "Test Issue",
			Description = "Test Description",
			Author = author
		};

		var command = new AddCommentCommand(
			issueId.ToString(),
			"Comment Title",
			"Comment Description",
			authorDto);

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		_commentRepository.AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var comment = callInfo.Arg<Comment>();
				return Result.Ok(comment);
			});

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Publish(
			Arg.Is<CommentAddedEvent>(e =>
				e.IssueId == issueId &&
				e.IssueTitle == issue.Title &&
				e.Comment.Title == "Comment Title"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task AddComment_WhenIssueNotFound_ReturnsError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var author = new UserInfo { Id = "user-123", Name = "Test User", Email = "test@example.com" };
		var authorDto = new UserDto(author);

		var command = new AddCommentCommand(
			issueId.ToString(),
			"Comment Title",
			"Comment Description",
			authorDto);

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found", ResultErrorCode.NotFound));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Issue not found");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);

		// Verify comment was never saved
		await _commentRepository.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
	}
}
