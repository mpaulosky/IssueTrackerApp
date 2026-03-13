// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssueCommentsQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Comments;

/// <summary>
///   Unit tests for GetIssueCommentsQueryHandler.
/// </summary>
public sealed class GetIssueCommentsQueryHandlerTests
{
	private readonly IRepository<Comment> _repository;
	private readonly ILogger<GetIssueCommentsQueryHandler> _logger;
	private readonly GetIssueCommentsQueryHandler _sut;

	public GetIssueCommentsQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Comment>>();
		_logger = Substitute.For<ILogger<GetIssueCommentsQueryHandler>>();
		_sut = new GetIssueCommentsQueryHandler(_repository, _logger);
	}

	[Fact]
	public async Task GetComments_ReturnsCommentsForIssue()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issueDto = new IssueDto(
			issueId,
			"Test Issue",
			"Test Description",
			DateTime.UtcNow,
			null,
			UserDto.Empty,
			CategoryDto.Empty,
			StatusDto.Empty,
			false,
			UserDto.Empty,
			false,
			false);

		var author = new UserDto("user-123", "Test User", "test@example.com");
		var comments = new List<Comment>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Comment 1",
				Description = "Description 1",
				Author = author,
				Issue = issueDto,
				DateCreated = DateTime.UtcNow.AddMinutes(-30),
				Archived = false
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Comment 2",
				Description = "Description 2",
				Author = author,
				Issue = issueDto,
				DateCreated = DateTime.UtcNow.AddMinutes(-15),
				Archived = false
			}
		};

		var query = new GetIssueCommentsQuery(issueId.ToString());

		_repository.FindAsync(Arg.Any<Expression<Func<Comment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Comment>>(comments));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().HaveCount(2);
		result.Value!.First().Title.Should().Be("Comment 2"); // More recent first
	}

	[Fact]
	public async Task GetComments_OrdersByCreatedAtDescending()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issueDto = new IssueDto(
			issueId,
			"Test Issue",
			"Test Description",
			DateTime.UtcNow,
			null,
			UserDto.Empty,
			CategoryDto.Empty,
			StatusDto.Empty,
			false,
			UserDto.Empty,
			false,
			false);

		var author = new UserDto("user-123", "Test User", "test@example.com");
		var oldestDate = DateTime.UtcNow.AddHours(-3);
		var middleDate = DateTime.UtcNow.AddHours(-2);
		var newestDate = DateTime.UtcNow.AddHours(-1);

		var comments = new List<Comment>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Oldest Comment",
				Description = "Oldest",
				Author = author,
				Issue = issueDto,
				DateCreated = oldestDate,
				Archived = false
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Newest Comment",
				Description = "Newest",
				Author = author,
				Issue = issueDto,
				DateCreated = newestDate,
				Archived = false
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Middle Comment",
				Description = "Middle",
				Author = author,
				Issue = issueDto,
				DateCreated = middleDate,
				Archived = false
			}
		};

		var query = new GetIssueCommentsQuery(issueId.ToString());

		_repository.FindAsync(Arg.Any<Expression<Func<Comment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Comment>>(comments));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().HaveCount(3);

		// Verify ordering: newest first
		var resultList = result.Value!.ToList();
		resultList[0].Title.Should().Be("Newest Comment");
		resultList[1].Title.Should().Be("Middle Comment");
		resultList[2].Title.Should().Be("Oldest Comment");
	}

	[Fact]
	public async Task GetComments_WhenNoComments_ReturnsEmptyList()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var query = new GetIssueCommentsQuery(issueId.ToString());

		_repository.FindAsync(Arg.Any<Expression<Func<Comment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Comment>>([]));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().BeEmpty();
	}

	[Fact]
	public async Task GetComments_WhenIncludeArchived_ReturnsArchivedComments()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issueDto = new IssueDto(
			issueId,
			"Test Issue",
			"Test Description",
			DateTime.UtcNow,
			null,
			UserDto.Empty,
			CategoryDto.Empty,
			StatusDto.Empty,
			false,
			UserDto.Empty,
			false,
			false);

		var author = new UserDto("user-123", "Test User", "test@example.com");
		var comments = new List<Comment>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Active Comment",
				Description = "Active",
				Author = author,
				Issue = issueDto,
				DateCreated = DateTime.UtcNow,
				Archived = false
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Archived Comment",
				Description = "Archived",
				Author = author,
				Issue = issueDto,
				DateCreated = DateTime.UtcNow.AddMinutes(-30),
				Archived = true
			}
		};

		var query = new GetIssueCommentsQuery(issueId.ToString(), IncludeArchived: true);

		_repository.FindAsync(Arg.Any<Expression<Func<Comment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Comment>>(comments));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}
}
