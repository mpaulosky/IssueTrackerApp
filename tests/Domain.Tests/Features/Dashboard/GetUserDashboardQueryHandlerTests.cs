// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetUserDashboardQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Dashboard.Queries;

namespace Domain.Tests.Features.Dashboard;

/// <summary>
///   Unit tests for <see cref="GetUserDashboardQueryHandler" />.
///   Verifies dashboard aggregation: stats calculation, filtering, and error propagation.
/// </summary>
public sealed class GetUserDashboardQueryHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly GetUserDashboardQueryHandler _handler;

	private const string UserId = "user-abc-123";

	public GetUserDashboardQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_handler = new GetUserDashboardQueryHandler(
			_repository,
			new NullLogger<GetUserDashboardQueryHandler>());
	}

	// -------------------------------------------------------
	// Happy-path
	// -------------------------------------------------------

	[Fact]
	public async Task Handle_WhenUserHasIssues_ReturnsDashboardWithCorrectTotals()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateIssue(UserId, "Open"),
			CreateIssue(UserId, "In Progress"),
			CreateIssue(UserId, "Resolved"),
			CreateIssue(UserId, "Closed"),
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		var query = new GetUserDashboardQuery(UserId);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalIssues.Should().Be(4);
		result.Value.OpenIssues.Should().Be(2);      // Open + In Progress
		result.Value.ResolvedIssues.Should().Be(2);  // Resolved + Closed
	}

	[Fact]
	public async Task Handle_WhenUserHasNoIssues_ReturnsZeroedDashboard()
	{
		// Arrange — repository returns issues for OTHER users only
		var issues = new List<Issue>
		{
			CreateIssue("other-user", "Open"),
			CreateIssue("other-user", "Resolved"),
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		var query = new GetUserDashboardQuery(UserId);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalIssues.Should().Be(0);
		result.Value.OpenIssues.Should().Be(0);
		result.Value.ResolvedIssues.Should().Be(0);
		result.Value.ThisWeekIssues.Should().Be(0);
		result.Value.RecentIssues.Should().BeEmpty();
	}

	[Fact]
	public async Task Handle_WhenRepositoryReturnsEmpty_ReturnsEmptyDashboard()
	{
		// Arrange
		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>([]));

		var query = new GetUserDashboardQuery(UserId);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalIssues.Should().Be(0);
		result.Value.RecentIssues.Should().BeEmpty();
	}

	// -------------------------------------------------------
	// Filtering
	// -------------------------------------------------------

	[Fact]
	public async Task Handle_ExcludesArchivedIssues_FromAllCounts()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateIssue(UserId, "Open",     archived: false, daysAgo: 1, title: "Active-Open"),   // counted
			CreateIssue(UserId, "Open",     archived: true,  daysAgo: 1, title: "Archived-Open"), // excluded
			CreateIssue(UserId, "Resolved", archived: false, daysAgo: 1, title: "Active-Resolved"), // counted
			CreateIssue(UserId, "Resolved", archived: true,  daysAgo: 1, title: "Archived-Resolved"), // excluded
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		var query = new GetUserDashboardQuery(UserId);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert — counts exclude archived
		result.Value!.TotalIssues.Should().Be(2);
		result.Value.OpenIssues.Should().Be(1);
		result.Value.ResolvedIssues.Should().Be(1);

		// RecentIssues also must not contain archived items
		result.Value.RecentIssues.Select(i => i.Title).Should().NotContain("Archived-Open");
		result.Value.RecentIssues.Select(i => i.Title).Should().NotContain("Archived-Resolved");
	}

	[Fact]
	public async Task Handle_ExcludesOtherUsersIssues_FromAllCounts()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateIssue(UserId,        "Open", daysAgo: 1, title: "My-Issue"),    // mine
			CreateIssue("stranger-456", "Open", daysAgo: 1, title: "Other-Issue"), // not mine
			CreateIssue("stranger-456", "Open", daysAgo: 1, title: "Other-Issue2"),// not mine
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		var query = new GetUserDashboardQuery(UserId);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert — totals only reflect my issues
		result.Value!.TotalIssues.Should().Be(1);
		result.Value.OpenIssues.Should().Be(1);

		// RecentIssues must not contain other-user issues
		result.Value.RecentIssues.Select(i => i.Title).Should().NotContain("Other-Issue");
		result.Value.RecentIssues.Select(i => i.Title).Should().NotContain("Other-Issue2");
	}

	// -------------------------------------------------------
	// Status counting
	// -------------------------------------------------------

	[Fact]
	public async Task Handle_CountsOpenAndInProgressAsOpen()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateIssue(UserId, "Open"),
			CreateIssue(UserId, "open"),          // case-insensitive
			CreateIssue(UserId, "In Progress"),
			CreateIssue(UserId, "IN PROGRESS"),   // case-insensitive
			CreateIssue(UserId, "Resolved"),      // NOT open
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(new GetUserDashboardQuery(UserId), CancellationToken.None);

		// Assert
		result.Value!.OpenIssues.Should().Be(4);
	}

	[Fact]
	public async Task Handle_CountsResolvedAndClosedAsResolved()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateIssue(UserId, "Resolved"),
			CreateIssue(UserId, "RESOLVED"),    // case-insensitive
			CreateIssue(UserId, "Closed"),
			CreateIssue(UserId, "closed"),      // case-insensitive
			CreateIssue(UserId, "Open"),        // NOT resolved
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(new GetUserDashboardQuery(UserId), CancellationToken.None);

		// Assert
		result.Value!.ResolvedIssues.Should().Be(4);
	}

	// -------------------------------------------------------
	// ThisWeek counter
	// -------------------------------------------------------

	[Fact]
	public async Task Handle_CountsOnlyIssuesCreatedWithinLastSevenDays()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateIssue(UserId, "Open", daysAgo: 0),   // today   → this week
			CreateIssue(UserId, "Open", daysAgo: 6),   // 6d ago  → this week
			CreateIssue(UserId, "Open", daysAgo: 8),   // 8d ago  → NOT this week
			CreateIssue(UserId, "Open", daysAgo: 30),  // month ago → NOT this week
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(new GetUserDashboardQuery(UserId), CancellationToken.None);

		// Assert — issues within 7-day window counted; older ones excluded
		result.Value!.ThisWeekIssues.Should().Be(2);
	}

	// -------------------------------------------------------
	// RecentIssues list
	// -------------------------------------------------------

	[Fact]
	public async Task Handle_RecentIssues_LimitedToTenMostRecent()
	{
		// Arrange — 15 issues for the user, created 0..14 days ago
		var issues = Enumerable.Range(0, 15)
			.Select(i => CreateIssue(UserId, "Open", daysAgo: i, title: $"Issue-{i:D2}"))
			.ToList();

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _handler.Handle(new GetUserDashboardQuery(UserId), CancellationToken.None);

		// Assert — count is capped, but TotalIssues reflects the full 15
		result.Value!.RecentIssues.Should().HaveCount(10);
		result.Value.TotalIssues.Should().Be(15);

		// The 10 returned should be the most recent (daysAgo 0–9)
		var returnedTitles = result.Value.RecentIssues.Select(i => i.Title).ToList();
		returnedTitles.Should().Contain("Issue-00");  // newest
		returnedTitles.Should().Contain("Issue-09");  // 10th newest
		returnedTitles.Should().NotContain("Issue-10"); // 11th — excluded
		returnedTitles.Should().NotContain("Issue-14"); // oldest — excluded
	}

	[Fact]
	public async Task Handle_RecentIssues_OrderedByDateCreatedDescending()
	{
		// Arrange — three issues created at different times
		var oldest  = CreateIssue(UserId, "Open", daysAgo: 10, title: "Oldest");
		var middle  = CreateIssue(UserId, "Open", daysAgo: 5,  title: "Middle");
		var newest  = CreateIssue(UserId, "Open", daysAgo: 1,  title: "Newest");

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(new[] { oldest, middle, newest }));

		// Act
		var result = await _handler.Handle(new GetUserDashboardQuery(UserId), CancellationToken.None);

		// Assert
		var titles = result.Value!.RecentIssues.Select(i => i.Title).ToList();
		titles[0].Should().Be("Newest");
		titles[1].Should().Be("Middle");
		titles[2].Should().Be("Oldest");
	}

	// -------------------------------------------------------
	// Error propagation
	// -------------------------------------------------------

	[Fact]
	public async Task Handle_WhenRepositoryFails_ReturnsFailureResult()
	{
		// Arrange
		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Issue>>("Database connection failed", ResultErrorCode.ExternalService));

		var query = new GetUserDashboardQuery(UserId);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Database connection failed");
	}

	[Fact]
	public async Task Handle_WhenRepositoryFails_PropagatesErrorCode()
	{
		// Arrange
		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Issue>>("error", ResultErrorCode.ExternalService));

		// Act
		var result = await _handler.Handle(new GetUserDashboardQuery(UserId), CancellationToken.None);

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.ExternalService);
	}

	[Fact]
	public async Task Handle_WhenRepositoryFails_DoesNotCallRepositoryAgain()
	{
		// Arrange
		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Issue>>("error"));

		// Act
		await _handler.Handle(new GetUserDashboardQuery(UserId), CancellationToken.None);

		// Assert — GetAllAsync called exactly once, nothing else
		await _repository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
	}

	// -------------------------------------------------------
	// Repository interaction
	// -------------------------------------------------------

	[Fact]
	public async Task Handle_AlwaysCallsGetAllAsync_ExactlyOnce()
	{
		// Arrange
		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>([]));

		// Act
		await _handler.Handle(new GetUserDashboardQuery(UserId), CancellationToken.None);

		// Assert
		await _repository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_PassesCancellationToken_ToRepository()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var token = cts.Token;

		_repository.GetAllAsync(token)
			.Returns(Result.Ok<IEnumerable<Issue>>([]));

		// Act
		await _handler.Handle(new GetUserDashboardQuery(UserId), token);

		// Assert
		await _repository.Received(1).GetAllAsync(token);
	}

	// -------------------------------------------------------
	// Helpers
	// -------------------------------------------------------

	/// <summary>
	///   Builds a minimal <see cref="Issue" /> suitable for handler tests.
	/// </summary>
	private static Issue CreateIssue(
		string authorId,
		string statusName,
		bool archived = false,
		int daysAgo = 1,
		string title = "Test Issue")
	{
		return new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = title,
			Description = "Test Description",
			Author = new UserInfo { Id = authorId, Name = "Test User", Email = "test@example.com" },
			Status = new StatusInfo { StatusName = statusName },
			Category = CategoryInfo.Empty,
			Archived = archived,
			DateCreated = DateTime.UtcNow.AddDays(-daysAgo),
			ApprovedForRelease = false,
			Rejected = false
		};
	}
}
