// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentMapperTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Mappers;

/// <summary>
///   Unit tests for the CommentMapper class.
///   Tests Comment to CommentDto and CommentDto to Comment conversions.
/// </summary>
public sealed class CommentMapperTests
{
	#region ToDto Tests

	[Fact]
	public void ToDto_WithValidComment_ReturnsCorrectDto()
	{
		// Arrange
		var comment = CreateTestComment();

		// Act
		var result = CommentMapper.ToDto(comment);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(comment.Id);
		result.Title.Should().Be(comment.Title);
		result.Description.Should().Be(comment.Description);
		result.DateCreated.Should().Be(comment.DateCreated);
		result.DateModified.Should().Be(comment.DateModified);
		result.IssueId.Should().Be(comment.IssueId);
		result.UserVotes.Should().BeEquivalentTo(comment.UserVotes);
		result.Archived.Should().Be(comment.Archived);
		result.IsAnswer.Should().Be(comment.IsAnswer);
	}

	[Fact]
	public void ToDto_WithNullComment_ReturnsEmptyDto()
	{
		// Arrange
		Comment? comment = null;

		// Act
		var result = CommentMapper.ToDto(comment);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.Title.Should().BeEmpty();
		result.Description.Should().BeEmpty();
		result.IssueId.Should().Be(ObjectId.Empty);
	}

	[Fact]
	public void ToDto_WithAuthor_MapsAuthorToUserDto()
	{
		// Arrange
		var comment = CreateTestComment();
		comment.Author = new UserInfo
		{
			Id = "auth0|123",
			Name = "Test Author",
			Email = "author@example.com"
		};

		// Act
		var result = CommentMapper.ToDto(comment);

		// Assert
		result.Author.Should().NotBeNull();
		result.Author.Id.Should().Be("auth0|123");
		result.Author.Name.Should().Be("Test Author");
		result.Author.Email.Should().Be("author@example.com");
	}

	[Fact]
	public void ToDto_WithArchivedBy_MapsArchivedByToUserDto()
	{
		// Arrange
		var comment = CreateTestComment();
		comment.Archived = true;
		comment.ArchivedBy = new UserInfo
		{
			Id = "auth0|456",
			Name = "Admin User",
			Email = "admin@example.com"
		};

		// Act
		var result = CommentMapper.ToDto(comment);

		// Assert
		result.ArchivedBy.Should().NotBeNull();
		result.ArchivedBy.Id.Should().Be("auth0|456");
		result.ArchivedBy.Name.Should().Be("Admin User");
		result.ArchivedBy.Email.Should().Be("admin@example.com");
	}

	[Fact]
	public void ToDto_WithAnswerSelectedBy_MapsAnswerSelectedByToUserDto()
	{
		// Arrange
		var comment = CreateTestComment();
		comment.IsAnswer = true;
		comment.AnswerSelectedBy = new UserInfo
		{
			Id = "auth0|789",
			Name = "Issue Owner",
			Email = "owner@example.com"
		};

		// Act
		var result = CommentMapper.ToDto(comment);

		// Assert
		result.IsAnswer.Should().BeTrue();
		result.AnswerSelectedBy.Should().NotBeNull();
		result.AnswerSelectedBy.Id.Should().Be("auth0|789");
		result.AnswerSelectedBy.Name.Should().Be("Issue Owner");
	}

	[Fact]
	public void ToDto_WithUserVotes_MapsVotesCorrectly()
	{
		// Arrange
		var comment = CreateTestComment();
		comment.UserVotes = ["user1", "user2", "user3"];

		// Act
		var result = CommentMapper.ToDto(comment);

		// Assert
		result.UserVotes.Should().HaveCount(3);
		result.UserVotes.Should().Contain("user1");
		result.UserVotes.Should().Contain("user2");
		result.UserVotes.Should().Contain("user3");
	}

	#endregion

	#region ToModel Tests

	[Fact]
	public void ToModel_WithValidDto_ReturnsCorrectModel()
	{
		// Arrange
		var dto = CreateTestCommentDto();

		// Act
		var result = CommentMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(dto.Id);
		result.Title.Should().Be(dto.Title);
		result.Description.Should().Be(dto.Description);
		result.DateCreated.Should().Be(dto.DateCreated);
		result.DateModified.Should().Be(dto.DateModified);
		result.IssueId.Should().Be(dto.IssueId);
		result.UserVotes.Should().BeEquivalentTo(dto.UserVotes);
		result.Archived.Should().Be(dto.Archived);
		result.IsAnswer.Should().Be(dto.IsAnswer);
	}

	[Fact]
	public void ToModel_WithNullDto_ReturnsEmptyModel()
	{
		// Arrange
		CommentDto? dto = null;

		// Act
		var result = CommentMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.Title.Should().BeEmpty();
		result.Description.Should().BeEmpty();
		result.IssueId.Should().Be(ObjectId.Empty);
	}

	[Fact]
	public void ToModel_WithAuthor_MapsAuthorToUserInfo()
	{
		// Arrange
		var dto = CreateTestCommentDto() with
		{
			Author = new UserDto("auth0|123", "Test Author", "author@example.com")
		};

		// Act
		var result = CommentMapper.ToModel(dto);

		// Assert
		result.Author.Should().NotBeNull();
		result.Author.Id.Should().Be("auth0|123");
		result.Author.Name.Should().Be("Test Author");
		result.Author.Email.Should().Be("author@example.com");
	}

	[Fact]
	public void ToModel_WithArchivedBy_MapsArchivedByToUserInfo()
	{
		// Arrange
		var dto = CreateTestCommentDto() with
		{
			Archived = true,
			ArchivedBy = new UserDto("auth0|456", "Admin User", "admin@example.com")
		};

		// Act
		var result = CommentMapper.ToModel(dto);

		// Assert
		result.Archived.Should().BeTrue();
		result.ArchivedBy.Should().NotBeNull();
		result.ArchivedBy.Id.Should().Be("auth0|456");
		result.ArchivedBy.Name.Should().Be("Admin User");
	}

	[Fact]
	public void ToModel_WithAnswerSelectedBy_MapsAnswerSelectedByToUserInfo()
	{
		// Arrange
		var dto = CreateTestCommentDto() with
		{
			IsAnswer = true,
			AnswerSelectedBy = new UserDto("auth0|789", "Issue Owner", "owner@example.com")
		};

		// Act
		var result = CommentMapper.ToModel(dto);

		// Assert
		result.IsAnswer.Should().BeTrue();
		result.AnswerSelectedBy.Should().NotBeNull();
		result.AnswerSelectedBy.Id.Should().Be("auth0|789");
	}

	#endregion

	#region ToDtoList Tests

	[Fact]
	public void ToDtoList_WithValidComments_ReturnsCorrectList()
	{
		// Arrange
		var comments = new List<Comment>
		{
			CreateTestComment("Comment 1"),
			CreateTestComment("Comment 2"),
			CreateTestComment("Comment 3")
		};

		// Act
		var result = CommentMapper.ToDtoList(comments);

		// Assert
		result.Should().HaveCount(3);
		result[0].Title.Should().Be("Comment 1");
		result[1].Title.Should().Be("Comment 2");
		result[2].Title.Should().Be("Comment 3");
	}

	[Fact]
	public void ToDtoList_WithNullCollection_ReturnsEmptyList()
	{
		// Arrange
		IEnumerable<Comment>? comments = null;

		// Act
		var result = CommentMapper.ToDtoList(comments);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_WithEmptyCollection_ReturnsEmptyList()
	{
		// Arrange
		var comments = new List<Comment>();

		// Act
		var result = CommentMapper.ToDtoList(comments);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_PreservesAllProperties()
	{
		// Arrange
		var comments = new List<Comment>
		{
			CreateTestComment("Test Comment")
		};
		comments[0].Archived = true;
		comments[0].IsAnswer = true;
		comments[0].UserVotes = ["vote1", "vote2"];

		// Act
		var result = CommentMapper.ToDtoList(comments);

		// Assert
		result.Should().HaveCount(1);
		result[0].Archived.Should().BeTrue();
		result[0].IsAnswer.Should().BeTrue();
		result[0].UserVotes.Should().HaveCount(2);
	}

	#endregion

	#region RoundTrip Tests

	[Fact]
	public void RoundTrip_ToDto_ThenToModel_PreservesData()
	{
		// Arrange
		var originalComment = CreateTestComment();
		originalComment.Title = "Round Trip Test";
		originalComment.Description = "Testing round trip conversion";
		originalComment.Archived = true;
		originalComment.IsAnswer = true;
		originalComment.UserVotes = ["user1", "user2"];

		// Act
		var dto = CommentMapper.ToDto(originalComment);
		var resultModel = CommentMapper.ToModel(dto);

		// Assert
		resultModel.Id.Should().Be(originalComment.Id);
		resultModel.Title.Should().Be(originalComment.Title);
		resultModel.Description.Should().Be(originalComment.Description);
		resultModel.IssueId.Should().Be(originalComment.IssueId);
		resultModel.Archived.Should().Be(originalComment.Archived);
		resultModel.IsAnswer.Should().Be(originalComment.IsAnswer);
		resultModel.UserVotes.Should().BeEquivalentTo(originalComment.UserVotes);
	}

	#endregion

	#region Helper Methods

	private static Comment CreateTestComment(string title = "Test Comment")
	{
		return new Comment
		{
			Id = ObjectId.GenerateNewId(),
			Title = title,
			Description = "Test Description",
			DateCreated = DateTime.UtcNow,
			DateModified = DateTime.UtcNow.AddHours(1),
			IssueId = ObjectId.GenerateNewId(),
			Author = UserInfo.Empty,
			UserVotes = [],
			Archived = false,
			ArchivedBy = UserInfo.Empty,
			IsAnswer = false,
			AnswerSelectedBy = UserInfo.Empty
		};
	}

	private static CommentDto CreateTestCommentDto(string title = "Test Comment DTO")
	{
		return new CommentDto(
			ObjectId.GenerateNewId(),
			title,
			"Test Description",
			DateTime.UtcNow,
			DateTime.UtcNow.AddHours(1),
			ObjectId.GenerateNewId(),
			UserDto.Empty,
			[],
			false,
			UserDto.Empty,
			false,
			UserDto.Empty);
	}

	#endregion
}
