// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LabelFilterChipTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Components.Pages.Issues;

using IssuesIndexPage = Web.Components.Pages.Issues.Index;

/// <summary>
///   Tests for label filter chip behaviour on the Issues Index page.
/// </summary>
public sealed class LabelFilterChipTests : BunitTestBase
{
	/// <summary>
	///   Builds a <see cref="PagedResult{T}"/> containing the supplied issues.
	/// </summary>
	private static PagedResult<IssueDto> BuildPagedResult(params IssueDto[] issues) =>
		PagedResult<IssueDto>.Create(issues, issues.Length, 1, 20);

	/// <summary>
	///   Creates a test issue that carries the given labels.
	/// </summary>
	private static IssueDto CreateIssueWithLabels(params string[] labels) =>
		CreateTestIssue() with { Labels = labels.ToList() };

	#region Label chip on issue card

	[Fact]
	public async Task LabelFilterChip_WhenLabelClicked_AddsToFilterState()
	{
		// Arrange — return one issue whose card has a "priority" label chip
		var issue = CreateIssueWithLabels("priority");
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(BuildPagedResult(issue))));

		var cut = Render<IssuesIndexPage>();

		// Wait for the async OnInitializedAsync to complete and issues to appear
		await cut.WaitForStateAsync(
			() => cut.Markup.Contains("priority"),
			TimeSpan.FromSeconds(3));

		// Act — click the label chip on the issue card
		var labelChip = cut.Find("button[title='Filter by this label']");
		await cut.InvokeAsync(() => labelChip.Click());

		// Assert — the active-filters bar is now visible
		await cut.WaitForStateAsync(
			() => cut.Markup.Contains("Active label filters:"),
			TimeSpan.FromSeconds(2));

		cut.Markup.Should().Contain("Active label filters:");
		cut.Markup.Should().Contain("priority");
	}

	#endregion

	#region Active filter chip removal

	[Fact]
	public async Task ActiveFilter_WhenXClicked_RemovesFilter()
	{
		// Arrange — navigate to a URL that pre-populates the label filter via query params
		Services.GetRequiredService<NavigationManager>().NavigateTo("/?label=bug");

		// SearchIssuesAsync is called on init; return empty so the page loads quickly
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(PagedResult<IssueDto>.Empty)));

		var cut = Render<IssuesIndexPage>();

		// Wait for the active-label-filter bar to be rendered (parsed from query params)
		await cut.WaitForStateAsync(
			() => cut.Markup.Contains("Active label filters:"),
			TimeSpan.FromSeconds(3));

		// Act — click the "bug" filter button (which also contains the × SVG)
		var filterChip = cut.Find("button.rounded-full");
		await cut.InvokeAsync(() => filterChip.Click());

		// Assert — active filter section is gone
		await cut.WaitForStateAsync(
			() => !cut.Markup.Contains("Active label filters:"),
			TimeSpan.FromSeconds(2));

		cut.Markup.Should().NotContain("Active label filters:");
	}

	[Fact]
	public async Task ActiveFilter_ClearAll_RemovesAllFilters()
	{
		// Arrange — pre-populate two label filters via URL query params
		Services.GetRequiredService<NavigationManager>().NavigateTo("/?label=bug,feature");

		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(PagedResult<IssueDto>.Empty)));

		var cut = Render<IssuesIndexPage>();

		// Wait for the active-label-filter bar to be rendered
		await cut.WaitForStateAsync(
			() => cut.Markup.Contains("Active label filters:"),
			TimeSpan.FromSeconds(3));

		// Both labels should appear as active chips
		cut.Markup.Should().Contain("bug");
		cut.Markup.Should().Contain("feature");

		// Act — click "Clear all"
		var clearAll = cut.Find("button.underline");
		await cut.InvokeAsync(() => clearAll.Click());

		// Assert — active filter section is gone
		await cut.WaitForStateAsync(
			() => !cut.Markup.Contains("Active label filters:"),
			TimeSpan.FromSeconds(2));

		cut.Markup.Should().NotContain("Active label filters:");
	}

	#endregion
}
