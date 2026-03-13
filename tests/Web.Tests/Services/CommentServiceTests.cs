// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for CommentService facade operations.
///   Tests comment CRUD orchestration and MediatR integration.
/// </summary>
public sealed class CommentServiceTests
{
	private readonly IMediator _mediator;
	private readonly Domain.Abstractions.INotificationService _notificationService;
	private readonly CommentService _sut;

	public CommentServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		_notificationService = Substitute.For<Domain.Abstractions.INotificationService>();
		_sut = new CommentService(_mediator, _notificationService);
	}

	#region GetCommentsAsync Tests

	[Fact]
	public async Task GetCommentsAsync_WithValidIssueId_ReturnsComments()
	{
		// Arrange
		var issueId = "issue-123";
		var comments = new List<CommentDto>
		{
			CreateTestCommentDto("Comment 1"),
			CreateTestCommentDto("Comment 2")
		};
		_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<CommentDto>>(comments));

		// Act
		var result = await _sut.GetCommentsAsync(issueId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetCommentsAsync_WithIncludeArchived_SendsCorrectQuery()
	{
		// Arrange
		var issueId = "issue-123";
		_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<CommentDto>>(new List<CommentDto>()));

		// Act
		await _sut.GetCommentsAsync(issueId, includeArchived: true);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<GetIssueCommentsQuery>(q =>
				q.IssueId == issueId &&
				q.IncludeArchived == true),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCommentsAsync_WhenMediatorFails_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IReadOnlyList<CommentDto>>("Issue not found"));

		// Act
		var result = await _sut.GetCommentsAsync("invalid-id");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task GetCommentsAsync_WithEmptyIssue_ReturnsEmptyList()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<CommentDto>>(new List<CommentDto>()));

		// Act
		var result = await _sut.GetCommentsAsync("issue-with-no-comments");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	#endregion

	#region AddCommentAsync Tests

	[Fact]
	public async Task AddCommentAsync_WithValidData_ReturnsCreatedComment()
	{
		// Arrange
		var issueId = "issue-123";
		var author = CreateTestUserDto();
		var createdComment = CreateTestCommentDto("New Comment");

		_mediator.Send(Arg.Any<AddCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdComment));

		// Act
		var result = await _sut.AddCommentAsync(issueId, "New Comment", "Description", author);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("New Comment");
	}

	[Fact]
	public async Task AddCommentAsync_WhenSuccessful_NotifiesClients()
	{
		// Arrange
		var createdComment = CreateTestCommentDto("New Comment");
		_mediator.Send(Arg.Any<AddCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdComment));

		// Act
		await _sut.AddCommentAsync("issue-123", "New Comment", "Description", CreateTestUserDto());

		// Assert
		await _notificationService.Received(1).NotifyCommentAddedAsync(
			Arg.Any<MongoDB.Bson.ObjectId>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Is<CommentDto>(c => c.Title == "New Comment"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task AddCommentAsync_WhenFails_DoesNotNotifyClients()
	{
		// Arrange
		_mediator.Send(Arg.Any<AddCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CommentDto>("Validation failed"));

		// Act
		await _sut.AddCommentAsync("issue-123", "", "Description", CreateTestUserDto());

		// Assert
		await _notificationService.DidNotReceive().NotifyCommentAddedAsync(
			Arg.Any<MongoDB.Bson.ObjectId>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CommentDto>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task AddCommentAsync_SendsCorrectCommand()
	{
		// Arrange
		var issueId = "issue-123";
		var author = CreateTestUserDto();
		var createdComment = CreateTestCommentDto("Test");
		_mediator.Send(Arg.Any<AddCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdComment));

		// Act
		await _sut.AddCommentAsync(issueId, "Title", "Description", author);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<AddCommentCommand>(c =>
				c.IssueId == issueId &&
				c.Title == "Title" &&
				c.Description == "Description" &&
				c.Author == author),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region UpdateCommentAsync Tests

	[Fact]
	public async Task UpdateCommentAsync_WithValidData_ReturnsUpdatedComment()
	{
		// Arrange
		var updatedComment = CreateTestCommentDto("Updated Comment");
		_mediator.Send(Arg.Any<UpdateCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedComment));

		// Act
		var result = await _sut.UpdateCommentAsync("comment-123", "Updated Comment", "New Description", "user1");

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("Updated Comment");
	}

	[Fact]
	public async Task UpdateCommentAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<UpdateCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CommentDto>("Comment not found"));

		// Act
		var result = await _sut.UpdateCommentAsync("invalid-id", "Title", "Desc", "user1");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task UpdateCommentAsync_WhenUnauthorized_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<UpdateCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CommentDto>("Unauthorized: Only the author can update this comment"));

		// Act
		var result = await _sut.UpdateCommentAsync("comment-123", "Title", "Desc", "different-user");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Unauthorized");
	}

	[Fact]
	public async Task UpdateCommentAsync_SendsCorrectCommand()
	{
		// Arrange
		var updatedComment = CreateTestCommentDto("Test");
		_mediator.Send(Arg.Any<UpdateCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedComment));

		// Act
		await _sut.UpdateCommentAsync("comment-123", "New Title", "New Desc", "user1");

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<UpdateCommentCommand>(c =>
				c.CommentId == "comment-123" &&
				c.Title == "New Title" &&
				c.Description == "New Desc" &&
				c.RequestingUserId == "user1"),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region DeleteCommentAsync Tests

	[Fact]
	public async Task DeleteCommentAsync_WithValidId_ReturnsSuccess()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		var result = await _sut.DeleteCommentAsync("comment-123", "user1", false, CreateTestUserDto());

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteCommentAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Comment not found"));

		// Act
		var result = await _sut.DeleteCommentAsync("invalid-id", "user1", false, CreateTestUserDto());

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task DeleteCommentAsync_WhenUnauthorized_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Unauthorized: Only the author or admin can delete this comment"));

		// Act
		var result = await _sut.DeleteCommentAsync("comment-123", "different-user", false, CreateTestUserDto());

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Unauthorized");
	}

	[Fact]
	public async Task DeleteCommentAsync_AsAdmin_SendsCorrectCommand()
	{
		// Arrange
		var archivedBy = CreateTestUserDto();
		_mediator.Send(Arg.Any<DeleteCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		await _sut.DeleteCommentAsync("comment-123", "admin-user", true, archivedBy);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<DeleteCommentCommand>(c =>
				c.CommentId == "comment-123" &&
				c.RequestingUserId == "admin-user" &&
				c.IsAdmin == true &&
				c.ArchivedBy == archivedBy),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteCommentAsync_AsOwner_SendsCorrectCommand()
	{
		// Arrange
		var archivedBy = CreateTestUserDto();
		_mediator.Send(Arg.Any<DeleteCommentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		await _sut.DeleteCommentAsync("comment-123", "user1", false, archivedBy);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<DeleteCommentCommand>(c =>
				c.CommentId == "comment-123" &&
				c.RequestingUserId == "user1" &&
				c.IsAdmin == false),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region Helper Methods

	private static CommentDto CreateTestCommentDto(string title)
	{
		var issueDto = IssueDto.Empty with
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Test Issue",
			Author = CreateTestUserDto()
		};

		return new CommentDto(
			ObjectId.GenerateNewId(),
			title,
			"Test Description",
			DateTime.UtcNow,
			null,
			issueDto,
			CreateTestUserDto(),
			new HashSet<string>(),
			false,
			UserDto.Empty,
			false,
			UserDto.Empty);
	}

	private static UserDto CreateTestUserDto()
	{
		return new UserDto("user1", "Test User", "test@example.com");
	}

	#endregion
}
