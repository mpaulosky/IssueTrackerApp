// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SearchIssuesQueryHandlerTests.cs
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
///   Unit tests for the <see cref="SearchIssuesQueryHandler" /> class.
/// </summary>
public sealed class SearchIssuesQueryHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<SearchIssuesQueryHandler> _logger;
	private readonly SearchIssuesQueryHandler _handler;

	public SearchIssuesQueryHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<SearchIssuesQueryHandler>>();
		_handler = new SearchIssuesQueryHandler(_issueRepository, _logger);
	}

	[Fact]
	public async Task SearchIssues_ByTitle_ReturnsMatches()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue("Login Bug", "Description 1"),
			CreateTestIssue("Payment Issue", "Description 2"),
			CreateTestIssue("Login Error", "Description 3"),
			CreateTestIssue("Dashboard Feature", "Description 4"),
			CreateTestIssue("Login Enhancement", "Description 5")
		};

		var request = new IssueSearchRequest { SearchText = "Login" };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(3);
		result.Value.TotalCount.Should().Be(3);
		result.Value.Items.Should().OnlyContain(i => i.Title.Contains("Login", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task SearchIssues_ByDescription_ReturnsMatches()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue("Issue 1", "User cannot authenticate properly"),
			CreateTestIssue("Issue 2", "Payment gateway timeout"),
			CreateTestIssue("Issue 3", "Authentication token expires too quickly"),
			CreateTestIssue("Issue 4", "Dashboard loading slowly"),
			CreateTestIssue("Issue 5", "Auth flow needs improvement")
		};

		var request = new IssueSearchRequest { SearchText = "auth" };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(3);
		result.Value.TotalCount.Should().Be(3);
	}

	[Fact]
	public async Task SearchIssues_WithPagination_WorksCorrectly()
	{
		// Arrange
		var issues = Enumerable.Range(1, 50)
			.Select(i => new Issue
			{
				Id = ObjectId.GenerateNewId(),
				Title = $"Searchable Issue {i}",
				Description = $"Description {i}",
				Category = CategoryDto.Empty,
				Author = UserDto.Empty,
				Status = StatusDto.Empty,
				DateCreated = DateTime.UtcNow.AddHours(-i),
				Archived = false,
				ApprovedForRelease = false,
				Rejected = false
			})
			.ToList();

		var request = new IssueSearchRequest
		{
			SearchText = "Searchable",
			Page = 2,
			PageSize = 10
		};
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(10);
		result.Value.TotalCount.Should().Be(50);
		result.Value.Page.Should().Be(2);
		result.Value.PageSize.Should().Be(10);
		result.Value.TotalPages.Should().Be(5);
		result.Value.HasPreviousPage.Should().BeTrue();
		result.Value.HasNextPage.Should().BeTrue();
	}

	[Fact]
	public async Task SearchIssues_WithStatusFilter_FiltersCorrectly()
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
			CreateTestIssue("Search Issue 1", "Description 1", status: openStatus),
			CreateTestIssue("Search Issue 2", "Description 2", status: closedStatus),
			CreateTestIssue("Search Issue 3", "Description 3", status: openStatus)
		};

		var request = new IssueSearchRequest { SearchText = "Search", StatusFilter = "Open" };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(2);
		result.Value.Items.Should().OnlyContain(i => i.Status.StatusName == "Open");
	}

	[Fact]
	public async Task SearchIssues_WithCategoryFilter_FiltersCorrectly()
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
			CreateTestIssue("Issue 1", "Description", category: bugCategory),
			CreateTestIssue("Issue 2", "Description", category: featureCategory),
			CreateTestIssue("Issue 3", "Description", category: bugCategory)
		};

		var request = new IssueSearchRequest { CategoryFilter = "Bug" };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(2);
		result.Value.Items.Should().OnlyContain(i => i.Category.CategoryName == "Bug");
	}

	[Fact]
	public async Task SearchIssues_WithAuthorFilter_FiltersCorrectly()
	{
		// Arrange
		var author1 = new UserDto("user-1", "John Doe", "john@example.com");
		var author2 = new UserDto("user-2", "Jane Doe", "jane@example.com");

		var issues = new List<Issue>
		{
			CreateTestIssue("Issue 1", "Description", author: author1),
			CreateTestIssue("Issue 2", "Description", author: author2),
			CreateTestIssue("Issue 3", "Description", author: author1),
			CreateTestIssue("Issue 4", "Description", author: author1)
		};

		var request = new IssueSearchRequest { AuthorId = "user-1" };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(3);
		result.Value.Items.Should().OnlyContain(i => i.Author.Id == "user-1");
	}

	[Fact]
	public async Task SearchIssues_WithDateRange_FiltersCorrectly()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue("Issue 1", "Desc", dateCreated: DateTime.UtcNow.AddDays(-10)),
			CreateTestIssue("Issue 2", "Desc", dateCreated: DateTime.UtcNow.AddDays(-5)),
			CreateTestIssue("Issue 3", "Desc", dateCreated: DateTime.UtcNow.AddDays(-2)),
			CreateTestIssue("Issue 4", "Desc", dateCreated: DateTime.UtcNow)
		};

		var request = new IssueSearchRequest
		{
			DateFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)),
			DateTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
		};
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(2);
	}

	[Fact]
	public async Task SearchIssues_ExcludesArchivedByDefault()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue("Active Issue 1", "Description", archived: false),
			CreateTestIssue("Archived Issue", "Description", archived: true),
			CreateTestIssue("Active Issue 2", "Description", archived: false)
		};

		var request = new IssueSearchRequest { IncludeArchived = false };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(2);
		result.Value.Items.Should().OnlyContain(i => !i.Archived);
	}

	[Fact]
	public async Task SearchIssues_WithIncludeArchived_ReturnsAllIssues()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue("Active Issue", "Description", archived: false),
			CreateTestIssue("Archived Issue", "Description", archived: true)
		};

		var request = new IssueSearchRequest { IncludeArchived = true };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(2);
	}

	[Fact]
	public async Task SearchIssues_WhenNoMatches_ReturnsEmptyResult()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue("Issue 1", "Description 1"),
			CreateTestIssue("Issue 2", "Description 2")
		};

		var request = new IssueSearchRequest { SearchText = "NonExistentTerm" };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().BeEmpty();
		result.Value.TotalCount.Should().Be(0);
	}

	[Fact]
	public async Task SearchIssues_WhenRepositoryFails_ReturnsFailure()
	{
		// Arrange
		var request = new IssueSearchRequest { SearchText = "Test" };
		var query = new SearchIssuesQuery(request);

		_issueRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Issue>>("Database error", ResultErrorCode.Conflict));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("error");
	}

	private static Issue CreateTestIssue(
		string title = "Test Issue",
		string description = "Test Description",
		StatusDto? status = null,
		CategoryDto? category = null,
		UserDto? author = null,
		bool archived = false,
		DateTime? dateCreated = null)
	{
		var issue = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = title,
			Description = description,
			Category = category ?? CategoryDto.Empty,
			Author = author ?? UserDto.Empty,
			Status = status ?? StatusDto.Empty,
			Archived = archived,
			ApprovedForRelease = false,
			Rejected = false
		};

		// Use reflection or direct property setting since DateCreated has init accessor
		if (dateCreated.HasValue)
		{
			// The Issue class has a public DateCreated property with init setter, so create a new instance
			return new Issue
			{
				Id = issue.Id,
				Title = issue.Title,
				Description = issue.Description,
				Category = issue.Category,
				Author = issue.Author,
				Status = issue.Status,
				DateCreated = dateCreated.Value,
				Archived = issue.Archived,
				ApprovedForRelease = issue.ApprovedForRelease,
				Rejected = issue.Rejected
			};
		}

		return issue;
	}
}
