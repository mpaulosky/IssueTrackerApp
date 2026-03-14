// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteCommentCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Comments;

/// <summary>
///   Unit tests for DeleteCommentCommandHandler.
/// </summary>
public sealed class DeleteCommentCommandHandlerTests
{
	private readonly IRepository<Comment> _repository;
	private readonly ILogger<DeleteCommentCommandHandler> _logger;
	private readonly DeleteCommentCommandHandler _sut;

	public DeleteCommentCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Comment>>();
		_logger = Substitute.For<ILogger<DeleteCommentCommandHandler>>();
		_sut = new DeleteCommentCommandHandler(_repository, _logger);
	}

	[Fact]
	public async Task DeleteComment_WhenExists_RemovesComment()
	{
		// Arrange
		var commentId = ObjectId.GenerateNewId();
		var owner = new UserInfo { Id = "user-123", Name = "Test User", Email = "test@example.com" };
		var existingComment = new Comment
		{
			Id = commentId,
			Title = "Test Comment",
			Description = "Test Description",
			Author = owner,
			IssueId = ObjectId.Empty,
			Archived = false
		};

		var archivedBy = new UserInfo { Id = "user-123", Name = "Test User", Email = "test@example.com" };
		var archivedByDto = new UserDto(archivedBy);
		var command = new DeleteCommentCommand(
			commentId.ToString(),
			owner.Id,
			false,
			archivedByDto);

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
		result.Value.Should().BeTrue();

		// Verify the comment was updated with archived status
		await _repository.Received(1).UpdateAsync(
			Arg.Is<Comment>(c =>
				c.Id == commentId &&
				c.Archived == true &&
				c.ArchivedBy.Id == archivedBy.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteComment_WhenNotFound_ReturnsError()
	{
		// Arrange
		var commentId = ObjectId.GenerateNewId();
		var archivedBy = new UserInfo { Id = "user-123", Name = "Test User", Email = "test@example.com" };
		var archivedByDto = new UserDto(archivedBy);
		var command = new DeleteCommentCommand(
			commentId.ToString(),
			"user-123",
			false,
			archivedByDto);

		_repository.GetByIdAsync(commentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Comment>("Comment not found", ResultErrorCode.NotFound));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Comment not found");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task DeleteComment_WhenAdmin_CanDeleteOthersComment()
	{
		// Arrange
		var commentId = ObjectId.GenerateNewId();
		var owner = new UserInfo { Id = "owner-123", Name = "Owner", Email = "owner@example.com" };
		var existingComment = new Comment
		{
			Id = commentId,
			Title = "Test Comment",
			Description = "Test Description",
			Author = owner,
			IssueId = ObjectId.Empty,
			Archived = false
		};

		var archivedBy = new UserInfo { Id = "admin-456", Name = "Admin User", Email = "admin@example.com" };
		var archivedByDto = new UserDto(archivedBy);
		var command = new DeleteCommentCommand(
			commentId.ToString(),
			"admin-456", // Different user but admin
			true, // IsAdmin = true
			archivedByDto);

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
	}

	[Fact]
	public async Task DeleteComment_WhenNotOwnerAndNotAdmin_ReturnsError()
	{
		// Arrange
		var commentId = ObjectId.GenerateNewId();
		var owner = new UserInfo { Id = "owner-123", Name = "Owner", Email = "owner@example.com" };
		var existingComment = new Comment
		{
			Id = commentId,
			Title = "Test Comment",
			Description = "Test Description",
			Author = owner,
			IssueId = ObjectId.Empty
		};

		var archivedBy = new UserInfo { Id = "other-user-456", Name = "Other User", Email = "other@example.com" };
		var archivedByDto = new UserDto(archivedBy);
		var command = new DeleteCommentCommand(
			commentId.ToString(),
			"other-user-456", // Different user
			false, // Not admin
			archivedByDto);

		_repository.GetByIdAsync(commentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingComment));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Only the comment author or an admin can delete this comment");
	}
}
