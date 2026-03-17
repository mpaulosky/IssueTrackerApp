// Issue Page Tests - bUnit Tests for Issue Razor Components
using Bunit;

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Issues.Queries;

using FluentAssertions;

using Microsoft.AspNetCore.Components;

using NSubstitute;

using Web.Components.Pages.Issues;

using Xunit;

using IssueIndex = Web.Components.Pages.Issues.Index;

namespace Web.Tests.Bunit.Pages.Issues;

public class IssueIndexPageTests : BunitTestBase
{
	[Fact]
	public async Task Index_RendersPageWithTitle()
	{
		var issues = new List<IssueDto>();
		var paginatedResult = new PaginatedResponse<IssueDto>(issues, 0, 1, 10);
		IssueService.GetIssuesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
		.Returns(Result.Ok<PaginatedResponse<IssueDto>>(paginatedResult));

		var cut = Render<IssueIndex>();

		cut.Find("h1").TextContent.Should().Contain("Issues");
		cut.Find("p").TextContent.Should().Contain("Manage and track all issues");
	}

	[Fact]
	public async Task Index_DisplaysEmptyStateWhenNoIssues()
	{
		var issues = new List<IssueDto>();
		var paginatedResult = new PaginatedResponse<IssueDto>(issues, 0, 1, 10);
		IssueService.GetIssuesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
		.Returns(Result.Ok<PaginatedResponse<IssueDto>>(paginatedResult));

		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		cut.Markup.Should().Contain("No issues found");
	}

	[Fact]
	public async Task Index_LoadsAndDisplaysIssues()
	{
		var issues = CreateTestIssues(5);
		var pagedResult = PagedResult<IssueDto>.Create(issues.AsReadOnly(), 5, 1, 10);
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
		.Returns(Result.Ok(pagedResult));

		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		cut.Markup.Should().Contain("Test Issue");
	}

	[Fact]
	public async Task Index_DisplaysErrorMessageOnLoadFailure()
	{
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
		.Returns(Result.Fail<PagedResult<IssueDto>>("Failed to load issues"));

		var cut = Render<IssueIndex>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		cut.Markup.Should().Contain("Error loading issues");
	}

	[Fact]
	public async Task Index_HasCreateIssueButton()
	{
		var issues = new List<IssueDto>();
		var paginatedResult = new PaginatedResponse<IssueDto>(issues, 0, 1, 10);
		IssueService.GetIssuesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
		.Returns(Result.Ok<PaginatedResponse<IssueDto>>(paginatedResult));

		var cut = Render<IssueIndex>();

		var createButton = cut.Find("a[href='/issues/create']");
		createButton.Should().NotBeNull();
		createButton.TextContent.Should().Contain("Create Issue");
	}
}

public class IssueCreatePageTests : BunitTestBase
{
	[Fact]
	public async Task Create_RendersFormWithTitle()
	{
		var categories = new List<CategoryDto> { CreateTestCategory() };
		var lookupService = Services.GetRequiredService<ILookupService>();
		lookupService.GetCategoriesAsync()
		.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		cut.Find("h1").TextContent.Should().Contain("Create New Issue");
		cut.Find("form").Should().NotBeNull();
	}

	[Fact]
	public async Task Create_RendersFormFields()
	{
		var categories = new List<CategoryDto> { CreateTestCategory() };
		var lookupService = Services.GetRequiredService<ILookupService>();
		lookupService.GetCategoriesAsync()
		.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		cut.Find("input#title").Should().NotBeNull();
		cut.Find("textarea#description").Should().NotBeNull();
		cut.Find("select#category").Should().NotBeNull();
	}

	[Fact]
	public async Task Create_LoadsCategoriesOnInitialization()
	{
		var categories = new List<CategoryDto>
{
CreateTestCategory(name: "Bug"),
CreateTestCategory(name: "Feature"),
CreateTestCategory(name: "Documentation")
};
		var lookupService = Services.GetRequiredService<ILookupService>();
		lookupService.GetCategoriesAsync()
		.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		var select = cut.Find("select#category");
		select.TextContent.Should().Contain("Bug");
		select.TextContent.Should().Contain("Feature");
		select.TextContent.Should().Contain("Documentation");
	}

	[Fact]
	public async Task Create_HasCancelButton()
	{
		var categories = new List<CategoryDto> { CreateTestCategory() };
		var lookupService = Services.GetRequiredService<ILookupService>();
		lookupService.GetCategoriesAsync()
		.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		var cancelLinks = cut.FindAll("a[href='/issues']");
		var cancelButton = cancelLinks.FirstOrDefault(a => a.TextContent.Contains("Cancel"));
		cancelButton.Should().NotBeNull();
		cancelButton!.TextContent.Should().Contain("Cancel");
	}

	[Fact]
	public async Task Create_HasSubmitButton()
	{
		var categories = new List<CategoryDto> { CreateTestCategory() };
		var lookupService = Services.GetRequiredService<ILookupService>();
		lookupService.GetCategoriesAsync()
		.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		var submitButton = cut.Find("button[type='submit']");
		submitButton.Should().NotBeNull();
		submitButton.TextContent.Should().Contain("Create Issue");
	}
}

public class IssueEditPageTests : BunitTestBase
{
	[Fact]
	public async Task Edit_RendersLoadingStateInitially()
	{
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		var categories = new List<CategoryDto> { CreateTestCategory() };

		var lookupService = Services.GetRequiredService<ILookupService>();
		lookupService.GetCategoriesAsync()
		.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Use a pending task so the component stays in loading state
		var tcs = new TaskCompletionSource<Result<IssueDto>>();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
		.Returns(tcs.Task);

		var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, issueId));

		cut.Markup.Should().Contain("animate-spin");
	}

	[Fact]
	public async Task Edit_LoadsAndDisplaysIssueDetails()
	{
		var issue = CreateTestIssue(title: "Sample Issue", description: "Sample Description");
		var issueId = issue.Id.ToString();
		var categories = new List<CategoryDto> { CreateTestCategory() };

		var lookupService = Services.GetRequiredService<ILookupService>();
		lookupService.GetCategoriesAsync()
		.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		IssueService.GetIssueByIdAsync(issueId)
		.Returns(Result.Ok<IssueDto>(issue));

		var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		cut.Find("h1").TextContent.Should().Contain("Edit Issue");
		cut.Markup.Should().Contain("Sample Issue");
	}

	[Fact]
	public async Task Edit_HasSubmitButton()
	{
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		var categories = new List<CategoryDto> { CreateTestCategory() };

		var lookupService = Services.GetRequiredService<ILookupService>();
		lookupService.GetCategoriesAsync()
		.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		IssueService.GetIssueByIdAsync(issueId)
		.Returns(Result.Ok<IssueDto>(issue));

		var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		var submitButton = cut.Find("button[type='submit']");
		submitButton.Should().NotBeNull();
		submitButton.TextContent.Should().Contain("Save Changes");
	}
}

public class IssueDetailsPageTests : BunitTestBase
{
	[Fact]
	public async Task Details_RendersLoadingStateInitially()
	{
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();

		// Use a pending task so the component stays in loading state
		var tcs = new TaskCompletionSource<Result<IssueDto>>();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
		.Returns(tcs.Task);

		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));

		cut.Markup.Should().Contain("animate-spin");
	}

	[Fact]
	public async Task Details_LoadsAndDisplaysIssueDetails()
	{
		var author = CreateTestUser(name: "John Doe", email: "john@example.com");
		var category = CreateTestCategory(name: "Bug");
		var status = CreateTestStatus(name: "Open");
		var issue = CreateTestIssue(
		title: "Critical Issue",
		description: "This is a critical issue",
		author: author,
		category: category,
		status: status
		);
		var issueId = issue.Id.ToString();

		IssueService.GetIssueByIdAsync(issueId)
		.Returns(Result.Ok<IssueDto>(issue));

		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		cut.Find("h1").TextContent.Should().Contain("Critical Issue");
		cut.Markup.Should().Contain("This is a critical issue");
		cut.Markup.Should().Contain("John Doe");
	}

	[Fact]
	public async Task Details_DisplaysEditButton()
	{
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();

		IssueService.GetIssueByIdAsync(issueId)
		.Returns(Result.Ok<IssueDto>(issue));

		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		var editLink = cut.FindAll("a").FirstOrDefault(a => a.GetAttribute("href")?.Contains("/edit") == true);
		editLink.Should().NotBeNull();
	}

	[Fact]
	public async Task Details_DisplaysMetaInformation()
	{
		var category = CreateTestCategory(name: "Feature Request");
		var status = CreateTestStatus(name: "In Progress");
		var issue = CreateTestIssue(category: category, status: status);
		var issueId = issue.Id.ToString();

		IssueService.GetIssueByIdAsync(issueId)
		.Returns(Result.Ok<IssueDto>(issue));

		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100)); // Wait for async loading

		cut.Markup.Should().Contain("Feature Request");
		cut.Markup.Should().Contain("In Progress");
	}
}
