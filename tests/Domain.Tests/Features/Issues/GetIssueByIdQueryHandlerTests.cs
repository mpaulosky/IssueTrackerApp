// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssueByIdQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;
using Domain.Features.Issues.Queries;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Issues;

/// <summary>
///   Unit tests for the <see cref="GetIssueByIdQueryHandler" /> class.
/// </summary>
public sealed class GetIssueByIdQueryHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<GetIssueByIdQueryHandler> _logger;
	private readonly GetIssueByIdQueryHandler _handler;

	public GetIssueByIdQueryHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<GetIssueByIdQueryHandler>>();
		_handler = new GetIssueByIdQueryHandler(_issueRepository, _logger);
	}

	[Fact]
	public async Task GetIssueById_WhenExists_ReturnsIssue()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var existingIssue = CreateTestIssue(issueId, "Found Issue", "Found Description");

		var query = new GetIssueByIdQuery(issueId.ToString());

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(issueId);
		result.Value.Title.Should().Be("Found Issue");
		result.Value.Description.Should().Be("Found Description");

		await _issueRepository.Received(1).GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssueById_WhenNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();

		var query = new GetIssueByIdQuery(issueId);

		_issueRepository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found", ResultErrorCode.NotFound));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Contain("not found");

		await _issueRepository.Received(1).GetByIdAsync(issueId, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssueById_WhenRepositoryReturnsNull_ReturnsNotFoundError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();

		var query = new GetIssueByIdQuery(issueId);

		_issueRepository.GetByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok<Issue>(null!));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task GetIssueById_ShouldReturnCompleteIssueData()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var category = new CategoryInfo
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Bug",
			CategoryDescription = "Bug category",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var author = new UserInfo { Id = "user-123", Name = "John Doe", Email = "john@example.com" };

		var status = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "In Progress",
			StatusDescription = "Issue is being worked on",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var existingIssue = new Issue
		{
			Id = issueId,
			Title = "Complete Issue",
			Description = "Complete Description",
			Category = category,
			Author = author,
			Status = status,
			DateCreated = DateTime.UtcNow.AddDays(-5),
			DateModified = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ApprovedForRelease = true,
			Rejected = false
		};

		var query = new GetIssueByIdQuery(issueId.ToString());

		_issueRepository.GetByIdAsync(issueId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingIssue));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(issueId);
		result.Value.Title.Should().Be("Complete Issue");
		result.Value.Description.Should().Be("Complete Description");
		result.Value.Category.Should().Be(CategoryMapper.ToDto(category));
		result.Value.Author.Should().Be(new UserDto(author));
		result.Value.Status.Should().Be(StatusMapper.ToDto(status));
		result.Value.ApprovedForRelease.Should().BeTrue();
		result.Value.Archived.Should().BeFalse();
	}

	[Fact]
	public async Task GetIssueById_WithEmptyId_StillQueriesRepository()
	{
		// Arrange
		var query = new GetIssueByIdQuery(string.Empty);

		_issueRepository.GetByIdAsync(string.Empty, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found", ResultErrorCode.NotFound));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();

		await _issueRepository.Received(1).GetByIdAsync(string.Empty, Arg.Any<CancellationToken>());
	}

	private static Issue CreateTestIssue(
		ObjectId id,
		string title = "Test Issue",
		string description = "Test Description")
	{
		return new Issue
		{
			Id = id,
			Title = title,
			Description = description,
			Category = CategoryInfo.Empty,
			Author = UserInfo.Empty,
			Status = StatusInfo.Empty,
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ApprovedForRelease = false,
			Rejected = false
		};
	}
}
