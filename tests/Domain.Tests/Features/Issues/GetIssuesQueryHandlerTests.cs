// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesQueryHandlerTests.cs
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
///   Unit tests for the <see cref="GetIssuesQueryHandler" /> class.
/// </summary>
public sealed class GetIssuesQueryHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<GetIssuesQueryHandler> _logger;
	private readonly GetIssuesQueryHandler _handler;

	public GetIssuesQueryHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<GetIssuesQueryHandler>>();
		_handler = new GetIssuesQueryHandler(_issueRepository, _logger);
	}

	[Fact]
	public async Task GetIssues_ReturnsPagedResult()
	{
		// Arrange
		var issues = CreateTestIssues(25); // Create 25 issues

		var query = new GetIssuesQuery(Page: 1, PageSize: 10);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(10);
		result.Value.Total.Should().Be(25);
		result.Value.Page.Should().Be(1);
		result.Value.PageSize.Should().Be(10);
		result.Value.TotalPages.Should().Be(3);
	}

	[Fact]
	public async Task GetIssues_WithStatusFilter_FiltersCorrectly()
	{
		// Arrange
		var openStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"Open",
			"Open status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var closedStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"Closed",
			"Closed status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var issues = new List<Issue>
		{
			CreateTestIssue(status: openStatus),
			CreateTestIssue(status: openStatus),
			CreateTestIssue(status: closedStatus),
			CreateTestIssue(status: openStatus),
			CreateTestIssue(status: closedStatus)
		};

		var query = new GetIssuesQuery(StatusFilter: "Open");

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(3);
		result.Value.Total.Should().Be(3);
		result.Value.Items.Should().OnlyContain(i => i.Status.StatusName == "Open");
	}

	[Fact]
	public async Task GetIssues_WithCategoryFilter_FiltersCorrectly()
	{
		// Arrange
		var bugCategory = new CategoryDto(
			ObjectId.GenerateNewId(),
			"Bug",
			"Bug category",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var featureCategory = new CategoryDto(
			ObjectId.GenerateNewId(),
			"Feature",
			"Feature category",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var issues = new List<Issue>
		{
			CreateTestIssue(category: bugCategory),
			CreateTestIssue(category: featureCategory),
			CreateTestIssue(category: bugCategory),
			CreateTestIssue(category: featureCategory),
			CreateTestIssue(category: featureCategory)
		};

		var query = new GetIssuesQuery(CategoryFilter: "Feature");

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(3);
		result.Value.Total.Should().Be(3);
		result.Value.Items.Should().OnlyContain(i => i.Category.CategoryName == "Feature");
	}

	[Fact]
	public async Task GetIssues_ExcludesArchivedByDefault()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue(archived: false),
			CreateTestIssue(archived: false),
			CreateTestIssue(archived: true),
			CreateTestIssue(archived: false),
			CreateTestIssue(archived: true)
		};

		var query = new GetIssuesQuery(); // IncludeArchived defaults to false

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(3);
		result.Value.Total.Should().Be(3);
		result.Value.Items.Should().OnlyContain(i => !i.Archived);
	}

	[Fact]
	public async Task GetIssues_WithIncludeArchived_ReturnsAllIssues()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue(archived: false),
			CreateTestIssue(archived: false),
			CreateTestIssue(archived: true),
			CreateTestIssue(archived: false),
			CreateTestIssue(archived: true)
		};

		var query = new GetIssuesQuery(IncludeArchived: true);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(5);
		result.Value.Total.Should().Be(5);
	}

	[Fact]
	public async Task GetIssues_ShouldOrderByDateCreatedDescending()
	{
		// Arrange
		var oldIssue = CreateTestIssueWithDate(DateTime.UtcNow.AddDays(-10));
		var newIssue = CreateTestIssueWithDate(DateTime.UtcNow);
		var middleIssue = CreateTestIssueWithDate(DateTime.UtcNow.AddDays(-5));

		var issues = new List<Issue> { oldIssue, newIssue, middleIssue };

		var query = new GetIssuesQuery();

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(3);
		result.Value.Items[0].DateCreated.Should().Be(newIssue.DateCreated);
		result.Value.Items[1].DateCreated.Should().Be(middleIssue.DateCreated);
		result.Value.Items[2].DateCreated.Should().Be(oldIssue.DateCreated);
	}

	[Fact]
	public async Task GetIssues_WithSecondPage_ReturnsCorrectItems()
	{
		// Arrange
		var issues = CreateTestIssues(25);

		var query = new GetIssuesQuery(Page: 2, PageSize: 10);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(10);
		result.Value.Page.Should().Be(2);
	}

	[Fact]
	public async Task GetIssues_WhenRepositoryFails_ReturnsFailure()
	{
		// Arrange
		var query = new GetIssuesQuery();

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Issue>>("Database error", ResultErrorCode.Conflict));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("error");
	}

	[Fact]
	public async Task GetIssues_WithEmptyRepository_ReturnsEmptyResult()
	{
		// Arrange
		var query = new GetIssuesQuery();

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>([]));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().BeEmpty();
		result.Value.Total.Should().Be(0);
	}

	private static List<Issue> CreateTestIssues(int count)
	{
		return Enumerable.Range(1, count)
			.Select(i => new Issue
			{
				Id = ObjectId.GenerateNewId(),
				Title = $"Issue {i}",
				Description = "Test Description",
				Category = CategoryDto.Empty,
				Author = UserDto.Empty,
				Status = StatusDto.Empty,
				DateCreated = DateTime.UtcNow.AddHours(-i),
				Archived = false,
				ApprovedForRelease = false,
				Rejected = false
			})
			.ToList();
	}

	private static Issue CreateTestIssueWithDate(DateTime dateCreated)
	{
		return new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Test Issue",
			Description = "Test Description",
			Category = CategoryDto.Empty,
			Author = UserDto.Empty,
			Status = StatusDto.Empty,
			DateCreated = dateCreated,
			Archived = false,
			ApprovedForRelease = false,
			Rejected = false
		};
	}

	private static Issue CreateTestIssue(
		StatusDto? status = null,
		CategoryDto? category = null,
		bool archived = false)
	{
		return new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Test Issue",
			Description = "Test Description",
			Category = category ?? CategoryDto.Empty,
			Author = UserDto.Empty,
			Status = status ?? StatusDto.Empty,
			DateCreated = DateTime.UtcNow,
			Archived = archived,
			ApprovedForRelease = false,
			Rejected = false
		};
	}
}
