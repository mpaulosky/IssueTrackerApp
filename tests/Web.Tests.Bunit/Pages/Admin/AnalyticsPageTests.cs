// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AnalyticsPageTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Domain.Features.Analytics.Queries;

using Microsoft.AspNetCore.Authorization;

using Web.Auth;

namespace Web.Tests.Bunit.Pages.Admin;

/// <summary>
///   Comprehensive bUnit tests for the Analytics admin page component.
///   Tests loading state, error state, success rendering, date range picker,
///   export functionality, authorization contract, and edge cases.
/// </summary>
public class AnalyticsPageTests : BunitTestBase
{
	// ─── Helpers ────────────────────────────────────────────────────────────────

	/// <summary>Creates a populated AnalyticsSummaryDto with sensible defaults.</summary>
	private static AnalyticsSummaryDto CreateSummary(
		int totalIssues = 100,
		int openIssues = 25,
		int closedIssues = 75,
		double averageResolutionHours = 48.0,
		IReadOnlyList<IssuesByStatusDto>? byStatus = null,
		IReadOnlyList<IssuesByCategoryDto>? byCategory = null,
		IReadOnlyList<IssuesOverTimeDto>? overTime = null,
		IReadOnlyList<TopContributorDto>? topContributors = null)
	{
		return new AnalyticsSummaryDto(
			TotalIssues: totalIssues,
			OpenIssues: openIssues,
			ClosedIssues: closedIssues,
			AverageResolutionHours: averageResolutionHours,
			ByStatus: byStatus ?? new List<IssuesByStatusDto>
			{
				new("Open", openIssues),
				new("Closed", closedIssues)
			},
			ByCategory: byCategory ?? new List<IssuesByCategoryDto>
			{
				new("Bug", 30),
				new("Feature", 50)
			},
			OverTime: overTime ?? new List<IssuesOverTimeDto>
			{
				new(DateTime.UtcNow.AddDays(-7), 10, 5),
				new(DateTime.UtcNow, 15, 12)
			},
			TopContributors: topContributors ?? new List<TopContributorDto>
			{
				new("u1", "Alice", 20, 8),
				new("u2", "Bob", 15, 5)
			});
	}

	/// <summary>Configures <see cref="Mediator"/> to return a successful analytics result.</summary>
	private void SetupAnalyticsSuccess(AnalyticsSummaryDto? dto = null)
	{
		var data = dto ?? CreateSummary();
		Mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(data)));
	}

	/// <summary>Configures <see cref="Mediator"/> to return a failed analytics result.</summary>
	private void SetupAnalyticsFailure(string error = "Service unavailable")
	{
		Mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<AnalyticsSummaryDto>(error)));
	}

	// ─── 1. Loading State ────────────────────────────────────────────────────────

	[Fact]
	public void Analytics_InitialRender_ShowsAnimatePulseSkeleton()
	{
		// Arrange – TaskCompletionSource that never resolves keeps the component in loading state
		SetupAuthenticatedUser(isAdmin: true);
		var tcs = new TaskCompletionSource<Result<AnalyticsSummaryDto>>();
		Mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<Analytics>();

		// Assert – animate-pulse skeleton cards must be visible while data is fetching
		cut.Markup.Should().Contain("animate-pulse",
			"skeleton loading cards should be rendered during the initial fetch");
	}

	[Fact]
	public void Analytics_LoadingState_RendersFourSkeletonCards()
	{
		// Arrange – pending TCS keeps _isLoading = true indefinitely
		SetupAuthenticatedUser(isAdmin: true);
		var tcs = new TaskCompletionSource<Result<AnalyticsSummaryDto>>();
		Mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<Analytics>();

		// Assert – the component hard-codes a loop of 4 skeleton cards
		var skeletonCards = cut.FindAll(".animate-pulse");
		skeletonCards.Should().HaveCount(4,
			"exactly four animate-pulse skeleton cards are rendered during loading");
	}

	[Fact]
	public async Task Analytics_AfterSuccessfulLoad_SkeletonCardsAreRemoved()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – skeleton must be gone once data has loaded
		cut.Markup.Should().NotContain("animate-pulse",
			"skeleton cards should be removed after the data fetch completes");
	}

	// ─── 2. Error State ──────────────────────────────────────────────────────────

	[Fact]
	public async Task Analytics_WhenServiceFails_ShowsExactErrorMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsFailure();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – matches the literal text in Analytics.razor's error block
		cut.Markup.Should().Contain("Failed to load analytics data. Please try again later.",
			"the error block should display the hard-coded error message verbatim");
	}

	[Fact]
	public async Task Analytics_WhenServiceFails_UsesRedErrorStyling()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsFailure();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – the error div carries bg-red-50 class
		cut.Markup.Should().Contain("bg-red-50",
			"error state should use Tailwind red background classes");
	}

	[Fact]
	public async Task Analytics_WhenServiceFails_SummaryCardTitlesAreAbsent()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsFailure();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – positive: the error block must be present (guards against blank-markup false pass)
		cut.Markup.Should().Contain("Failed to load analytics data",
			"the error block must be present to confirm the error branch rendered");

		// Assert – negative: summary content must not bleed through the error branch
		cut.Markup.Should().NotContain("Total Issues",
			"the summary cards should not be rendered when loading fails");
		cut.Markup.Should().NotContain("Export to CSV",
			"the export button should not be rendered when loading fails");
	}

	// ─── 3. Success State – Summary Cards ────────────────────────────────────────

	[Fact]
	public async Task Analytics_Success_RendersTotalIssuesCount()
	{
		// Arrange – use a distinctive number unlikely to appear elsewhere in the markup
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess(CreateSummary(totalIssues: 142));

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – find the specific SummaryCard that contains "Total Issues" and verify
		// its value element shows 142. This scopes the assertion to the right card rather
		// than relying on a global substring match.
		var allCards = cut.FindAll(".card-bordered.p-6");
		var totalCard = allCards.FirstOrDefault(c => c.TextContent.Contains("Total Issues"));
		totalCard.Should().NotBeNull("Total Issues summary card should be rendered");
		totalCard!.TextContent.Should().Contain("142",
			"Total Issues card should display the TotalIssues value");
	}

	[Fact]
	public async Task Analytics_Success_RendersOpenIssuesCount()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess(CreateSummary(openIssues: 37));

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – scoped to the Open Issues card element
		var allCards = cut.FindAll(".card-bordered.p-6");
		var openCard = allCards.FirstOrDefault(c => c.TextContent.Contains("Open Issues"));
		openCard.Should().NotBeNull("Open Issues summary card should be rendered");
		openCard!.TextContent.Should().Contain("37",
			"Open Issues card should display the OpenIssues value");
	}

	[Fact]
	public async Task Analytics_Success_RendersClosedIssuesCount()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess(CreateSummary(closedIssues: 88));

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – scoped to the Closed Issues card element
		var allCards = cut.FindAll(".card-bordered.p-6");
		var closedCard = allCards.FirstOrDefault(c => c.TextContent.Contains("Closed Issues"));
		closedCard.Should().NotBeNull("Closed Issues summary card should be rendered");
		closedCard!.TextContent.Should().Contain("88",
			"Closed Issues card should display the ClosedIssues value");
	}

	[Fact]
	public async Task Analytics_Success_FormatsResolutionTimeInHours()
	{
		// Arrange – 12.5 hours → FormatResolutionTime returns "12.5h"
		// (hours >= 1 && hours < 24 branch: $"{hours:F1}h")
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess(CreateSummary(averageResolutionHours: 12.5));

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("12.5h",
			"12.5 hours should be formatted as '12.5h'");
		cut.Markup.Should().Contain("Avg Resolution Time",
			"Avg Resolution Time card title should be present");
	}

	[Fact]
	public async Task Analytics_Success_FormatsResolutionTimeInDays()
	{
		// Arrange – 48.0 hours → FormatResolutionTime returns "2.0d"
		// (hours >= 24 branch: $"{hours / 24:F1}d")
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess(CreateSummary(averageResolutionHours: 48.0));

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("2.0d",
			"48.0 hours should be formatted as '2.0d'");
	}

	[Fact]
	public async Task Analytics_Success_RendersFourSummaryCardTitles()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – all four SummaryCard titles must appear in the rendered markup
		var markup = cut.Markup;
		markup.Should().Contain("Total Issues", "Total Issues card should be present");
		markup.Should().Contain("Open Issues", "Open Issues card should be present");
		markup.Should().Contain("Closed Issues", "Closed Issues card should be present");
		markup.Should().Contain("Avg Resolution Time", "Avg Resolution Time card should be present");
	}

	// ─── 4. Date Range Picker ────────────────────────────────────────────────────

	[Fact]
	public async Task Analytics_Success_DateRangePickerRendersTwoDateInputs()
	{
		// Arrange – DateRangePicker is outside the @if/_isLoading branches so it
		// must always render regardless of data state.
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var dateInputs = cut.FindAll("input[type='date']");
		dateInputs.Should().HaveCount(2,
			"DateRangePicker should render two date inputs (From and To)");
	}

	[Fact]
	public void Analytics_LoadingState_DateRangePickerStillRenders()
	{
		// Arrange – pending TCS keeps component in loading state
		SetupAuthenticatedUser(isAdmin: true);
		var tcs = new TaskCompletionSource<Result<AnalyticsSummaryDto>>();
		Mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<Analytics>();

		// Assert – DateRangePicker is rendered outside the conditional block,
		// so it must be visible even during loading.
		var dateInputs = cut.FindAll("input[type='date']");
		dateInputs.Should().HaveCount(2,
			"DateRangePicker should render even while data is still loading");
	}

	// ─── 5. Export Button ────────────────────────────────────────────────────────

	[Fact]
	public async Task Analytics_Success_RendersExportToCsvButton()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Export to CSV",
			"export button should be visible in the success state");
	}

	[Fact]
	public async Task Analytics_Success_ExportButtonIsEnabledBeforeExport()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – the button must not carry a disabled attribute before any export is triggered
		var exportButton = cut.FindAll("button")
			.First(b => b.TextContent.Contains("Export to CSV", StringComparison.OrdinalIgnoreCase));
		exportButton.GetAttribute("disabled").Should().BeNull(
			"export button should be enabled when _isExporting is false");
	}

	// ─── 6. Authorization Contract ───────────────────────────────────────────────

	[Fact]
	public void Analytics_ComponentClass_HasAdminPolicyAuthorizeAttribute()
	{
		// bUnit's direct Render<T>() bypasses route-level authorization enforcement,
		// so we verify the [Authorize(Policy = AdminPolicy)] attribute is actually
		// declared on the compiled class via reflection.
		var authorizeAttr = typeof(Analytics)
			.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
			.Cast<AuthorizeAttribute>()
			.FirstOrDefault();

		authorizeAttr.Should().NotBeNull(
			"Analytics page must carry [Authorize] to protect it from non-admin access");
		authorizeAttr!.Policy.Should().Be(AuthorizationPolicies.AdminPolicy,
			"Analytics must require the AdminPolicy specifically");
	}

	// ─── 7. Mediator Interaction ─────────────────────────────────────────────────

	[Fact]
	public async Task Analytics_OnInitialize_InvokesMediatorExactlyOnce()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess();

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – OnInitializedAsync calls LoadAnalyticsData once; DateRangePicker
		// does NOT call OnRangeChanged again because startDate/endDate are already set.
		await Mediator.Received(1)
			.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>());
	}

	// ─── 8. Edge Cases ───────────────────────────────────────────────────────────

	[Fact]
	public async Task Analytics_EmptyDataCollections_RendersWithoutException()
	{
		// Arrange – all IReadOnlyList properties are empty; PrepareChartData must
		// gracefully handle LINQ over empty sequences without throwing.
		SetupAuthenticatedUser(isAdmin: true);
		var emptyDto = new AnalyticsSummaryDto(
			TotalIssues: 0,
			OpenIssues: 0,
			ClosedIssues: 0,
			AverageResolutionHours: 0.0,
			ByStatus: new List<IssuesByStatusDto>(),
			ByCategory: new List<IssuesByCategoryDto>(),
			OverTime: new List<IssuesOverTimeDto>(),
			TopContributors: new List<TopContributorDto>());
		SetupAnalyticsSuccess(emptyDto);

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – component renders in success state and all card titles are present
		var markup = cut.Markup;
		markup.Should().Contain("Total Issues",
			"summary cards should render even when all collection properties are empty");
		markup.Should().Contain("Open Issues");
		markup.Should().Contain("Closed Issues");
		markup.Should().Contain("Avg Resolution Time");
	}

	[Fact]
	public async Task Analytics_SubHourResolutionTime_FormatsInMinutes()
	{
		// Arrange – 0.5 hours → FormatResolutionTime returns "30m"
		// (hours < 1 branch: $"{hours * 60:F0}m")
		SetupAuthenticatedUser(isAdmin: true);
		SetupAnalyticsSuccess(CreateSummary(averageResolutionHours: 0.5));

		// Act
		var cut = Render<Analytics>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert – "30m" is a substring unique to the resolution card value.
		// "Last 30 days" in DateRangePicker does NOT contain "30m", so no false positives.
		cut.Markup.Should().Contain("30m",
			"sub-hour resolution time of 0.5 hours should be formatted as '30m'");
	}
}
