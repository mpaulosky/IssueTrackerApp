// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateCommentCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Comments;

/// <summary>
///   Unit tests for UpdateCommentCommandHandler.
/// </summary>
public sealed class UpdateCommentCommandHandlerTests
{
	private readonly IRepository<Comment> _repository;
	private readonly ILogger<UpdateCommentCommandHandler> _logger;
	private readonly UpdateCommentCommandHandler _sut;

	public UpdateCommentCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Comment>>();
		_logger = Substitute.For<ILogger<UpdateCommentCommandHandler>>();
		_sut = new UpdateCommentCommandHandler(_repository, _logger);
	}

	[Fact]
	public async Task UpdateComment_WhenExists_UpdatesContent()
	{
		// Arrange
		var commentId = ObjectId.GenerateNewId();
		var author = new UserDto("user-123", "Test User", "test@example.com");
		var existingComment = new Comment
		{
			Id = commentId,
			Title = "Original Title",
			Description = "Original Description",
			Author = author,
			Issue = IssueDto.Empty,
			DateCreated = DateTime.UtcNow.AddHours(-1)
		};

		var command = new UpdateCommentCommand(
			commentId.ToString(),
			"Updated Title",
			"Updated Description",
			author.Id);

		_repository.GetByIdAsync(commentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingComment));

		_repository.UpdateAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>())
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
		result.Value!.Title.Should().Be("Updated Title");
		result.Value.Description.Should().Be("Updated Description");
		result.Value.DateModified.Should().NotBeNull();
	}

	[Fact]
	public async Task UpdateComment_WhenNotFound_ReturnsError()
	{
		// Arrange
		var commentId = ObjectId.GenerateNewId();

		var command = new UpdateCommentCommand(
			commentId.ToString(),
			"Updated Title",
			"Updated Description",
			"user-123");

		_repository.GetByIdAsync(commentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Comment>("Comment not found", ResultErrorCode.NotFound));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Comment not found");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);

		// Verify update was never called
		await _repository.DidNotReceive().UpdateAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdateComment_WhenNotOwner_ReturnsError()
	{
		// Arrange
		var commentId = ObjectId.GenerateNewId();
		var owner = new UserDto("owner-123", "Owner", "owner@example.com");
		var existingComment = new Comment
		{
			Id = commentId,
			Title = "Original Title",
			Description = "Original Description",
			Author = owner,
			Issue = IssueDto.Empty
		};

		var command = new UpdateCommentCommand(
			commentId.ToString(),
			"Updated Title",
			"Updated Description",
			"other-user-456"); // Different user

		_repository.GetByIdAsync(commentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingComment));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Only the comment author can edit this comment");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}
}
