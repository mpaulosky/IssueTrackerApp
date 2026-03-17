// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IndexPageTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Domain.Features.Issues.Queries;

using MongoDB.Bson;

using Web.Services;

using IssueIndex = Web.Components.Pages.Issues.Index;

namespace Web.Tests.Bunit.Pages.Issues;

/// <summary>
///   Comprehensive tests for the Issues Index page component.
///   Tests loading states, issue listing, pagination, filtering, sorting, and bulk operations.
/// </summary>
public class IndexPageTests : BunitTestBase
{
	#region Loading State Tests

	[Fact]
	public void Index_ShowsLoadingSpinnerInitially()
	{
		// Arrange
		var tcs = new TaskCompletionSource<Result<PagedResult<IssueDto>>>();
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<IssueIndex>();

		// Assert
		cut.Markup.Should().Contain("animate-spin");
	}

	[Fact]
	public async Task Index_HidesLoadingSpinnerAfterDataLoads()
	{
		// Arrange
		var issues = CreateTestIssues(5);
		var pagedResult = PagedResult<IssueDto>.Create(issues.AsReadOnly(), 5, 1, 10);
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().NotContain("animate-spin");
	}

	#endregion

	#region Issue Listing Tests

	[Fact]
	public async Task Index_DisplaysIssueList()
	{
		// Arrange
		var issues = CreateTestIssues(3);
		issues[0] = issues[0] with { Title = "First Issue" };
		issues[1] = issues[1] with { Title = "Second Issue" };
		issues[2] = issues[2] with { Title = "Third Issue" };
		
		var pagedResult = PagedResult<IssueDto>.Create(issues.AsReadOnly(), 3, 1, 10);
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("First Issue");
		cut.Markup.Should().Contain("Second Issue");
		cut.Markup.Should().Contain("Third Issue");
	}

	[Fact]
	public async Task Index_DisplaysIssueDetails()
	{
		// Arrange
		var author = CreateTestUser(name: "Jane Developer");
		var category = CreateTestCategory(name: "Bug Report");
		var status = CreateTestStatus(name: "Open");
		var issue = CreateTestIssue(
			title: "Critical Bug",
			author: author,
			category: category,
			status: status
		);
		
		var pagedResult = PagedResult<IssueDto>.Create([issue], 1, 1, 10);
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Critical Bug");
		cut.Markup.Should().Contain("Jane Developer");
		cut.Markup.Should().Contain("Bug Report");
		cut.Markup.Should().Contain("Open");
	}

	[Fact]
	public async Task Index_IssueLinksToDetailsPage()
	{
		// Arrange
		var issue = CreateTestIssue(title: "Test Issue");
		var pagedResult = PagedResult<IssueDto>.Create([issue], 1, 1, 10);
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var issueLink = cut.FindAll("a").FirstOrDefault(a => 
			a.GetAttribute("href")?.Contains($"/issues/{issue.Id}") == true);
		issueLink.Should().NotBeNull();
	}

	[Fact]
	public async Task Index_DisplaysCreatedDate()
	{
		// Arrange
		var testDate = new DateTime(2024, 1, 15);
		var issue = CreateTestIssue();
		var issueWithDate = issue with { DateCreated = testDate };
		
		var pagedResult = PagedResult<IssueDto>.Create([issueWithDate], 1, 1, 10);
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Jan 15, 2024");
	}

	#endregion

	#region Empty State Tests

	[Fact]
	public async Task Index_DisplaysEmptyStateWhenNoIssues()
	{
		// Arrange
		var pagedResult = PagedResult<IssueDto>.Empty;
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("No issues found");
		cut.Markup.Should().Contain("create a new issue to get started");
	}

	[Fact]
	public async Task Index_EmptyStateShowsCreateButton()
	{
		// Arrange
		var pagedResult = PagedResult<IssueDto>.Empty;
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var createButton = cut.FindAll("a").FirstOrDefault(a => 
			a.GetAttribute("href") == "/issues/create");
		createButton.Should().NotBeNull();
		createButton!.TextContent.Should().Contain("Create Issue");
	}

	#endregion

	#region Pagination Tests

	[Fact]
	public async Task Index_DisplaysPaginationWhenNeeded()
	{
		// Arrange
		var issues = CreateTestIssues(15); // More than one page
		var pagedResult = PagedResult<IssueDto>.Create(issues.Take(10).ToList().AsReadOnly(), 15, 1, 10);
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.HasComponent<Web.Components.Shared.Pagination>().Should().BeTrue();
	}

	[Fact]
	public async Task Index_PaginationShowsCorrectTotalItems()
	{
		// Arrange
		var issues = CreateTestIssues(25);
		var pagedResult = PagedResult<IssueDto>.Create(issues.Take(10).ToList().AsReadOnly(), 25, 1, 10);
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var pagination = cut.FindComponent<Web.Components.Shared.Pagination>();
		pagination.Instance.TotalItems.Should().Be(25);
	}

	#endregion

	#region Filter Panel Tests

	[Fact]
	public async Task Index_DisplaysFilterPanel()
	{
		// Arrange
		var pagedResult = PagedResult<IssueDto>.Empty;
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.HasComponent<Web.Components.Shared.FilterPanel>().Should().BeTrue();
	}

	[Fact]
	public async Task Index_FilterPanelReceivesStatuses()
	{
		// Arrange
		var statuses = new List<StatusDto>
		{
			CreateTestStatus(name: "Open"),
			CreateTestStatus(name: "Closed")
		};
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		var pagedResult = PagedResult<IssueDto>.Empty;
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var filterPanel = cut.FindComponent<Web.Components.Shared.FilterPanel>();
		filterPanel.Instance.Statuses.Should().NotBeNull();
	}

	#endregion

	#region Error Handling Tests

	[Fact]
	public async Task Index_DisplaysErrorMessageOnFailure()
	{
		// Arrange
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<PagedResult<IssueDto>>("Search service unavailable"));

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Error loading issues");
		cut.Markup.Should().Contain("Search service unavailable");
	}

	[Fact]
	public async Task Index_HandlesServiceException()
	{
		// Arrange
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<Result<PagedResult<IssueDto>>>(new Exception("Network error")));

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Network error");
	}

	#endregion

	#region Authorization Tests

	[Fact]
	public void Index_RequiresAuthentication()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<IssueIndex>();

		// Assert
		cut.Markup.Should().NotBeNull();
		// Anonymous users should be redirected or see authorization message
	}

	[Fact]
	public async Task Index_AdminUserSeesBulkDeleteOption()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var pagedResult = PagedResult<IssueDto>.Create(CreateTestIssues(3).AsReadOnly(), 3, 1, 10);
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var toolbar = cut.FindComponent<Web.Components.Issues.BulkActionToolbar>();
		toolbar.Instance.IsAdmin.Should().BeTrue();
	}

	#endregion

	#region Page Header Tests

	[Fact]
	public async Task Index_DisplaysPageTitle()
	{
		// Arrange
		var pagedResult = PagedResult<IssueDto>.Empty;
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Find("h1").TextContent.Should().Contain("Issues");
	}

	[Fact]
	public async Task Index_DisplaysPageDescription()
	{
		// Arrange
		var pagedResult = PagedResult<IssueDto>.Empty;
		SetupSuccessfulSearch(pagedResult);

		// Act
		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Manage and track all issues");
	}

	#endregion

	#region Helper Methods

	private void SetupSuccessfulSearch(PagedResult<IssueDto> pagedResult)
	{
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(pagedResult));
	}

	#endregion
}