// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SharedComponentTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Domain.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Web.Components.Shared;
using Web.Services;

namespace Web.Tests.Bunit.Shared;

/// <summary>
///   Tests for the Pagination component.
/// </summary>
public class PaginationTests : BunitTestBase
{
	[Fact]
	public void Pagination_WithSinglePage_DoesNotRender()
	{
		// Arrange & Act
		var cut = Render<Pagination>(parameters => parameters
			.Add(p => p.CurrentPage, 1)
			.Add(p => p.TotalPages, 1)
			.Add(p => p.TotalItems, 10)
			.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, _ => { })));

		// Assert
		cut.Markup.Should().NotContain("<nav");
	}

	[Fact]
	public void Pagination_WithMultiplePages_RendersNavigation()
	{
		// Arrange & Act
		var cut = Render<Pagination>(parameters => parameters
			.Add(p => p.CurrentPage, 1)
			.Add(p => p.TotalPages, 5)
			.Add(p => p.TotalItems, 50)
			.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, _ => { })));

		// Assert
		cut.Find("nav").Should().NotBeNull();
		cut.Markup.Should().Contain("Showing page 1 of 5");
	}

	[Fact]
	public void Pagination_OnFirstPage_PreviousButtonNotRendered()
	{
		// Arrange & Act
		var cut = Render<Pagination>(parameters => parameters
			.Add(p => p.CurrentPage, 1)
			.Add(p => p.TotalPages, 5)
			.Add(p => p.TotalItems, 50)
			.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, _ => { })));

		// Assert
		cut.Markup.Should().NotContain("Previous");
	}

	[Fact]
	public void Pagination_OnLastPage_NextButtonNotRendered()
	{
		// Arrange & Act
		var cut = Render<Pagination>(parameters => parameters
			.Add(p => p.CurrentPage, 5)
			.Add(p => p.TotalPages, 5)
			.Add(p => p.TotalItems, 50)
			.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, _ => { })));

		// Assert
		cut.Markup.Should().NotContain("Next");
	}

	[Fact]
	public void Pagination_OnMiddlePage_BothButtonsRendered()
	{
		// Arrange & Act
		var cut = Render<Pagination>(parameters => parameters
			.Add(p => p.CurrentPage, 3)
			.Add(p => p.TotalPages, 5)
			.Add(p => p.TotalItems, 50)
			.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, _ => { })));

		// Assert
		cut.Markup.Should().Contain("Previous");
		cut.Markup.Should().Contain("Next");
	}

	[Fact]
	public async Task Pagination_ClickNextPage_InvokesCallback()
	{
		// Arrange
		var pageChanged = false;
		var newPage = 0;

		var cut = Render<Pagination>(parameters => parameters
			.Add(p => p.CurrentPage, 1)
			.Add(p => p.TotalPages, 5)
			.Add(p => p.TotalItems, 50)
			.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, page =>
			{
				pageChanged = true;
				newPage = page;
				return Task.CompletedTask;
			})));

		// Act
		var nextButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Next"));
		nextButton?.Click();

		// Assert
		pageChanged.Should().BeTrue();
		newPage.Should().Be(2);
	}

	[Fact]
	public void Pagination_CurrentPageHighlighted_WithPrimaryColor()
	{
		// Arrange & Act
		var cut = Render<Pagination>(parameters => parameters
			.Add(p => p.CurrentPage, 3)
			.Add(p => p.TotalPages, 5)
			.Add(p => p.TotalItems, 50)
			.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, _ => { })));

		// Assert
		cut.Markup.Should().Contain("border-primary-500");
		cut.Markup.Should().Contain("text-primary-600");
	}
}

/// <summary>
///   Tests for the FilterPanel component.
/// </summary>
public class FilterPanelTests : BunitTestBase
{
	[Fact]
	public void FilterPanel_RendersSearchInput()
	{
		// Arrange & Act
		var cut = Render<FilterPanel>(parameters => parameters
			.Add(p => p.SearchText, null)
			.Add(p => p.SearchTextChanged, EventCallback.Factory.Create<string?>(this, _ => { }))
			.Add(p => p.OnFiltersApplied, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnFiltersCleared, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.FindComponent<SearchInput>().Should().NotBeNull();
	}

	[Fact]
	public void FilterPanel_WithoutActiveFilters_ClearButtonNotVisible()
	{
		// Arrange & Act
		var cut = Render<FilterPanel>(parameters => parameters
			.Add(p => p.SearchText, null)
			.Add(p => p.StatusFilter, null)
			.Add(p => p.CategoryFilter, null)
			.Add(p => p.DateFrom, null)
			.Add(p => p.DateTo, null)
			.Add(p => p.OnFiltersApplied, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnFiltersCleared, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().NotContain("Clear All");
	}

	[Fact]
	public void FilterPanel_WithActiveFilters_DisplaysActiveFilterCount()
	{
		// Arrange & Act
		var cut = Render<FilterPanel>(parameters => parameters
			.Add(p => p.SearchText, "bug")
			.Add(p => p.StatusFilter, "Open")
			.Add(p => p.CategoryFilter, "Feature")
			.Add(p => p.DateFrom, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)))
			.Add(p => p.DateTo, DateOnly.FromDateTime(DateTime.UtcNow))
			.Add(p => p.OnFiltersApplied, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnFiltersCleared, EventCallback.Factory.Create(this, () => { })));

		// Assert - 5 active filters: SearchText, StatusFilter, CategoryFilter, DateFrom, DateTo
		cut.Markup.Should().Contain("5 active");
	}

	[Fact]
	public async Task FilterPanel_ToggleFilters_ShowsHidesFilterOptions()
	{
		// Arrange
		var cut = Render<FilterPanel>(parameters => parameters
			.Add(p => p.SearchText, null)
			.Add(p => p.OnFiltersApplied, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnFiltersCleared, EventCallback.Factory.Create(this, () => { })));

		// Act - Toggle filters open using aria-controls selector (aria-expanded is only present when true)
		var toggleButton = cut.Find("button[aria-controls='filter-options']");
		toggleButton.Click();

		// Assert
		cut.Markup.Should().Contain("filter-options");
	}

	[Fact]
	public void FilterPanel_WithStatuses_RendersStatusOptions()
	{
		// Arrange
		var statuses = new List<StatusDto>
		{
			CreateTestStatus(name: "Open"),
			CreateTestStatus(name: "In Progress"),
			CreateTestStatus(name: "Closed")
		};

		// Act
		var cut = Render<FilterPanel>(parameters => parameters
			.Add(p => p.Statuses, statuses)
			.Add(p => p.OnFiltersApplied, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnFiltersCleared, EventCallback.Factory.Create(this, () => { })));

		// Expand filters to see status options
		var toggleButton = cut.Find("button[aria-controls='filter-options']");
		toggleButton.Click();

		// Assert
		cut.Markup.Should().Contain("Open");
		cut.Markup.Should().Contain("In Progress");
		cut.Markup.Should().Contain("Closed");
	}

	[Fact]
	public void FilterPanel_WithCategories_RendersCategoryOptions()
	{
		// Arrange
		var categories = new List<CategoryDto>
		{
			CreateTestCategory(name: "Bug"),
			CreateTestCategory(name: "Feature"),
			CreateTestCategory(name: "Documentation")
		};

		// Act
		var cut = Render<FilterPanel>(parameters => parameters
			.Add(p => p.Categories, categories)
			.Add(p => p.OnFiltersApplied, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnFiltersCleared, EventCallback.Factory.Create(this, () => { })));

		// Expand filters to see category options
		var toggleButton = cut.Find("button[aria-controls='filter-options']");
		toggleButton.Click();

		// Assert
		cut.Markup.Should().Contain("Bug");
		cut.Markup.Should().Contain("Feature");
		cut.Markup.Should().Contain("Documentation");
	}

	[Fact]
	public void FilterPanel_DisplaysTotalResults()
	{
		// Arrange & Act
		var cut = Render<FilterPanel>(parameters => parameters
			.Add(p => p.TotalResults, 42)
			.Add(p => p.OnFiltersApplied, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnFiltersCleared, EventCallback.Factory.Create(this, () => { })));

		// Expand filters to see total results
		var toggleButton = cut.Find("button[aria-controls='filter-options']");
		toggleButton.Click();

		// Assert
		cut.Markup.Should().Contain("42 results found");
	}
}

/// <summary>
///   Tests for the StatusBadge component.
/// </summary>
public class StatusBadgeTests : BunitTestBase
{
	[Fact]
	public void StatusBadge_WithStatus_RendersStatusName()
	{
		// Arrange
		var status = CreateTestStatus(name: "Open");

		// Act
		var cut = Render<StatusBadge>(parameters => parameters
			.Add(p => p.Status, status));

		// Assert
		cut.Find("span").TextContent.Should().Contain("Open");
	}

	[Fact]
	public void StatusBadge_WithNullStatus_RendersUnknown()
	{
		// Arrange & Act
		var cut = Render<StatusBadge>(parameters => parameters
			.Add(p => p.Status, null));

		// Assert
		cut.Find("span").TextContent.Should().Contain("Unknown");
	}

	[Fact]
	public void StatusBadge_OpenStatus_AppliesBlueColorClass()
	{
		// Arrange
		var status = CreateTestStatus(name: "Open");

		// Act
		var cut = Render<StatusBadge>(parameters => parameters
			.Add(p => p.Status, status));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("bg-blue-100");
		cut.Find("span").GetAttribute("class").Should().Contain("text-blue-800");
	}

	[Fact]
	public void StatusBadge_InProgressStatus_AppliesYellowColorClass()
	{
		// Arrange
		var status = CreateTestStatus(name: "In Progress");

		// Act
		var cut = Render<StatusBadge>(parameters => parameters
			.Add(p => p.Status, status));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("bg-yellow-100");
		cut.Find("span").GetAttribute("class").Should().Contain("text-yellow-800");
	}

	[Fact]
	public void StatusBadge_ResolvedStatus_AppliesGreenColorClass()
	{
		// Arrange
		var status = CreateTestStatus(name: "Resolved");

		// Act
		var cut = Render<StatusBadge>(parameters => parameters
			.Add(p => p.Status, status));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("bg-green-100");
		cut.Find("span").GetAttribute("class").Should().Contain("text-green-800");
	}

	[Fact]
	public void StatusBadge_ClosedStatus_AppliesGrayColorClass()
	{
		// Arrange
		var status = CreateTestStatus(name: "Closed");

		// Act
		var cut = Render<StatusBadge>(parameters => parameters
			.Add(p => p.Status, status));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("bg-gray-100");
		cut.Find("span").GetAttribute("class").Should().Contain("text-gray-800");
	}

	[Fact]
	public void StatusBadge_WithAdditionalClasses_IncludesThemInOutput()
	{
		// Arrange
		var status = CreateTestStatus(name: "Open");

		// Act
		var cut = Render<StatusBadge>(parameters => parameters
			.Add(p => p.Status, status)
			.Add(p => p.AdditionalClasses, "custom-class"));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("custom-class");
	}
}

/// <summary>
///   Tests for the CategoryBadge component.
/// </summary>
public class CategoryBadgeTests : BunitTestBase
{
	[Fact]
	public void CategoryBadge_WithCategory_RendersCategoryName()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");

		// Act
		var cut = Render<CategoryBadge>(parameters => parameters
			.Add(p => p.Category, category));

		// Assert
		cut.Find("span").TextContent.Should().Contain("Bug");
	}

	[Fact]
	public void CategoryBadge_WithNullCategory_RendersUnknown()
	{
		// Arrange & Act
		var cut = Render<CategoryBadge>(parameters => parameters
			.Add(p => p.Category, null));

		// Assert
		cut.Find("span").TextContent.Should().Contain("Unknown");
	}

	[Fact]
	public void CategoryBadge_BugCategory_AppliesRedColorClass()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");

		// Act
		var cut = Render<CategoryBadge>(parameters => parameters
			.Add(p => p.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("bg-red-100");
		cut.Find("span").GetAttribute("class").Should().Contain("text-red-800");
	}

	[Fact]
	public void CategoryBadge_FeatureCategory_AppliesGreenColorClass()
	{
		// Arrange
		var category = CreateTestCategory(name: "Feature");

		// Act
		var cut = Render<CategoryBadge>(parameters => parameters
			.Add(p => p.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("bg-green-100");
		cut.Find("span").GetAttribute("class").Should().Contain("text-green-800");
	}

	[Fact]
	public void CategoryBadge_EnhancementCategory_AppliesBlueColorClass()
	{
		// Arrange
		var category = CreateTestCategory(name: "Enhancement");

		// Act
		var cut = Render<CategoryBadge>(parameters => parameters
			.Add(p => p.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("bg-blue-100");
		cut.Find("span").GetAttribute("class").Should().Contain("text-blue-800");
	}

	[Fact]
	public void CategoryBadge_QuestionCategory_AppliesPurpleColorClass()
	{
		// Arrange
		var category = CreateTestCategory(name: "Question");

		// Act
		var cut = Render<CategoryBadge>(parameters => parameters
			.Add(p => p.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("bg-purple-100");
		cut.Find("span").GetAttribute("class").Should().Contain("text-purple-800");
	}

	[Fact]
	public void CategoryBadge_WithAdditionalClasses_IncludesThemInOutput()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");

		// Act
		var cut = Render<CategoryBadge>(parameters => parameters
			.Add(p => p.Category, category)
			.Add(p => p.AdditionalClasses, "custom-class"));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("custom-class");
	}
}

/// <summary>
///   Tests for the SearchInput component.
/// </summary>
public class SearchInputTests : BunitTestBase
{
	[Fact]
	public void SearchInput_RendersWithCorrectId()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(parameters => parameters
			.Add(p => p.Id, "test-search")
			.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

		// Assert
		cut.Find("input").GetAttribute("id").Should().Be("test-search");
	}

	[Fact]
	public void SearchInput_RendersWithCorrectPlaceholder()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(parameters => parameters
			.Add(p => p.Placeholder, "Search issues...")
			.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

		// Assert
		cut.Find("input").GetAttribute("placeholder").Should().Be("Search issues...");
	}

	[Fact]
	public void SearchInput_WithValue_DisplaysValue()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(parameters => parameters
			.Add(p => p.Value, "test query")
			.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

		// Assert
		cut.Find("input").GetAttribute("value").Should().Be("test query");
	}

	[Fact]
	public void SearchInput_WithValue_ShowsClearButton()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(parameters => parameters
			.Add(p => p.Value, "test query")
			.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

		// Assert
		cut.FindAll("button").Should().NotBeEmpty();
		cut.Markup.Should().Contain("Clear search");
	}

	[Fact]
	public void SearchInput_WithoutValue_HidesClearButton()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(parameters => parameters
			.Add(p => p.Value, null)
			.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

		// Assert
		cut.FindAll("button").Should().BeEmpty();
	}

	[Fact]
	public async Task SearchInput_ClearButton_ClearsValue()
	{
		// Arrange
		var clearedValue = "";
		var cut = Render<SearchInput>(parameters => parameters
			.Add(p => p.Value, "test query")
			.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, value =>
			{
				clearedValue = value ?? "";
				return Task.CompletedTask;
			})));

		// Act
		var clearButton = cut.Find("button");
		clearButton.Click();

		// Assert
		clearedValue.Should().Be("");
	}

	[Fact]
	public void SearchInput_RendersWithAriaLabel()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(parameters => parameters
			.Add(p => p.AriaLabel, "Search issues")
			.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

		// Assert
		cut.Find("input").GetAttribute("aria-label").Should().Be("Search issues");
	}
}

/// <summary>
///   Tests for the SummaryCard component.
/// </summary>
public class SummaryCardTests : BunitTestBase
{
	[Fact]
	public void SummaryCard_RendersTitle()
	{
		// Arrange & Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42"));

		// Assert
		cut.Markup.Should().Contain("Total Issues");
	}

	[Fact]
	public void SummaryCard_RendersValue()
	{
		// Arrange & Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42"));

		// Assert
		cut.Markup.Should().Contain("42");
	}

	[Fact]
	public void SummaryCard_WithSubtitle_RendersSubtitle()
	{
		// Arrange & Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42")
			.Add(p => p.Subtitle, "This month"));

		// Assert
		cut.Markup.Should().Contain("This month");
	}

	[Fact]
	public void SummaryCard_WithoutSubtitle_DoesNotRenderSubtitle()
	{
		// Arrange & Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42")
			.Add(p => p.Subtitle, null));

		// Assert
		cut.Markup.Should().NotContain("This month");
	}

	[Fact]
	public void SummaryCard_WithIcon_RendersIcon()
	{
		// Arrange
		var iconSvg = "<svg class=\"w-5 h-5\"></svg>";

		// Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42")
			.Add(p => p.Icon, iconSvg));

		// Assert
		cut.Markup.Should().Contain("svg");
	}

	[Fact]
	public void SummaryCard_PositiveTrend_ShowsUpArrow()
	{
		// Arrange & Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42")
			.Add(p => p.TrendPercentage, 15.5));

		// Assert
		cut.Markup.Should().Contain("+15.5%");
		cut.Markup.Should().Contain("text-green-500");
	}

	[Fact]
	public void SummaryCard_NegativeTrend_ShowsDownArrow()
	{
		// Arrange & Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42")
			.Add(p => p.TrendPercentage, -10.2));

		// Assert
		cut.Markup.Should().Contain("-10.2%");
		cut.Markup.Should().Contain("text-red-500");
	}

	[Fact]
	public void SummaryCard_ZeroTrend_ShowsNoChange()
	{
		// Arrange & Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42")
			.Add(p => p.TrendPercentage, 0));

		// Assert
		cut.Markup.Should().Contain("No change");
	}

	[Fact]
	public void SummaryCard_WithCustomIconBackground_AppliesCustomClass()
	{
		// Arrange & Act
		var cut = Render<SummaryCard>(parameters => parameters
			.Add(p => p.Title, "Total Issues")
			.Add(p => p.Value, "42")
			.Add(p => p.Icon, "<svg></svg>")
			.Add(p => p.IconBackgroundClass, "bg-red-100 dark:bg-red-900"));

		// Assert
		cut.Markup.Should().Contain("bg-red-100");
	}
}

/// <summary>
///   Tests for the ToastContainer component.
/// </summary>
public class ToastContainerTests : BunitTestBase
{
	[Fact]
	public void ToastContainer_Renders()
	{
		// Arrange & Act
		var toastService = Services.GetRequiredService<ToastService>();
		var cut = Render<ToastContainer>();

		// Assert
		cut.Should().NotBeNull();
		cut.Find("div").GetAttribute("class").Should().Contain("fixed");
	}

	[Fact]
	public void ToastContainer_WithNoToasts_DoesNotDisplayAnyMessages()
	{
		// Arrange & Act
		var cut = Render<ToastContainer>();

		// Assert
		cut.Markup.Should().NotContain("role=\"alert\"");
	}

	[Fact]
	public async Task ToastContainer_WithInfoToast_DisplaysWithInfoStyles()
	{
		// Arrange
		var toastService = Services.GetRequiredService<ToastService>();
		var cut = Render<ToastContainer>();

		// Act - Must use InvokeAsync since Show triggers StateHasChanged
		await cut.InvokeAsync(() => toastService.ShowInfo("Test Info Message"));

		// Assert
		cut.Markup.Should().Contain("Test Info Message");
		cut.Markup.Should().Contain("bg-blue-50");
	}

	[Fact]
	public async Task ToastContainer_WithSuccessToast_DisplaysWithSuccessStyles()
	{
		// Arrange
		var toastService = Services.GetRequiredService<ToastService>();
		var cut = Render<ToastContainer>();

		// Act
		await cut.InvokeAsync(() => toastService.ShowSuccess("Test Success Message"));

		// Assert
		cut.Markup.Should().Contain("Test Success Message");
		cut.Markup.Should().Contain("bg-green-50");
	}

	[Fact]
	public async Task ToastContainer_WithWarningToast_DisplaysWithWarningStyles()
	{
		// Arrange
		var toastService = Services.GetRequiredService<ToastService>();
		var cut = Render<ToastContainer>();

		// Act
		await cut.InvokeAsync(() => toastService.ShowWarning("Test Warning Message"));

		// Assert
		cut.Markup.Should().Contain("Test Warning Message");
		cut.Markup.Should().Contain("bg-yellow-50");
	}

	[Fact]
	public async Task ToastContainer_WithErrorToast_DisplaysWithErrorStyles()
	{
		// Arrange
		var toastService = Services.GetRequiredService<ToastService>();
		var cut = Render<ToastContainer>();

		// Act
		await cut.InvokeAsync(() => toastService.ShowError("Test Error Message"));

		// Assert
		cut.Markup.Should().Contain("Test Error Message");
		cut.Markup.Should().Contain("bg-red-50");
	}

	[Fact]
	public async Task ToastContainer_AllToasts_HaveDismissButton()
	{
		// Arrange
		var toastService = Services.GetRequiredService<ToastService>();
		var cut = Render<ToastContainer>();

		// Act
		await cut.InvokeAsync(() => toastService.ShowInfo("Test Message"));

		// Assert
		cut.Markup.Should().Contain("role=\"alert\"");
		var closeButtons = cut.FindAll("button[type=\"button\"]");
		closeButtons.Should().NotBeEmpty();
	}
}

/// <summary>
///   Tests for the SignalRConnection component.
/// </summary>
public class SignalRConnectionTests : BunitTestBase
{
	[Fact]
	public void SignalRConnection_Renders()
	{
		// Arrange & Act
		var cut = Render<SignalRConnection>();

		// Assert
		cut.Should().NotBeNull();
	}

	[Fact]
	public void SignalRConnection_WithDisconnectedState_ShowsDisconnectedStatus()
	{
		// Arrange
		var signalRClient = Services.GetRequiredService<SignalRClientService>();

		// Act
		var cut = Render<SignalRConnection>();

		// Assert
		cut.Markup.Should().Contain("Reconnecting...");
	}

	[Fact]
	public void SignalRConnection_StatusIndicator_HasAppropriateTitle()
	{
		// Arrange & Act
		var cut = Render<SignalRConnection>();

		// Assert
		cut.Markup.Should().Contain("title=");
	}

	[Fact]
	public void SignalRConnection_FixedPositioning_AppliedCorrectly()
	{
		// Arrange & Act
		var cut = Render<SignalRConnection>();

		// Assert
		cut.Find("div").GetAttribute("class").Should().Contain("fixed");
		cut.Find("div").GetAttribute("class").Should().Contain("bottom-4");
		cut.Find("div").GetAttribute("class").Should().Contain("right-4");
	}
}

/// <summary>
///   Tests for the FileUpload component.
/// </summary>
public class FileUploadTests : BunitTestBase
{
	[Fact]
	public void FileUpload_RendersUploadZone()
	{
		// Arrange & Act
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, _ => { })));

		// Assert
		cut.Markup.Should().Contain("Upload a file");
		cut.Markup.Should().Contain("drag and drop");
	}

	[Fact]
	public void FileUpload_RendersWithCorrectAcceptTypes()
	{
		// Arrange & Act
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, _ => { })));

		// Assert
		cut.Markup.Should().Contain("JPG, PNG, GIF, WEBP, PDF, TXT, MD");
	}

	[Fact]
	public void FileUpload_WithoutError_ErrorMessageNotShown()
	{
		// Arrange & Act
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, _ => { })));

		// Assert
		cut.Markup.Should().NotContain("bg-red-50");
	}

	[Fact]
	public void FileUpload_FileUploadLabel_ClickableForAccessibility()
	{
		// Arrange & Act
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, _ => { })));

		// Assert - The clickable label is the second label with cursor-pointer class
		var label = cut.Find("label.cursor-pointer");
		label.Should().NotBeNull();
		label.TextContent.Should().Contain("Upload a file");
	}

	[Fact]
	public void FileUpload_HasInputFile_WithFileUploadComponent()
	{
		// Arrange & Act
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, _ => { })));

		// Assert - InputFile renders as <input type="file">
		cut.Find("input[type='file']").Should().NotBeNull();
	}

	[Fact]
	public void FileUpload_AttachmentsLabel_Displayed()
	{
		// Arrange & Act
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, _ => { })));

		// Assert
		cut.Markup.Should().Contain("Attachments");
	}
}

/// <summary>
///   Tests for the DeleteConfirmationModal component.
/// </summary>
public class DeleteConfirmationModalTests : BunitTestBase
{
	[Fact]
	public void DeleteConfirmationModal_WhenHidden_DoesNotRender()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, false)
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().NotContain("role=\"dialog\"");
	}

	[Fact]
	public void DeleteConfirmationModal_WhenVisible_RendersDialog()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().Contain("role=\"dialog\"");
	}

	[Fact]
	public void DeleteConfirmationModal_RendersTitle()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Title, "Delete Issue?")
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().Contain("Delete Issue?");
	}

	[Fact]
	public void DeleteConfirmationModal_RendersMessage()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Message, "This cannot be undone.")
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().Contain("This cannot be undone.");
	}

	[Fact]
	public void DeleteConfirmationModal_WithItemTitle_RendersItemTitle()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.ItemTitle, "Critical Bug")
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().Contain("Critical Bug");
	}

	[Fact]
	public void DeleteConfirmationModal_ConfirmButton_DisplaysConfirmText()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.ConfirmButtonText, "Delete Forever")
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().Contain("Delete Forever");
	}

	[Fact]
	public void DeleteConfirmationModal_CancelButton_DisplaysCancelText()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.CancelButtonText, "Keep It")
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().Contain("Keep It");
	}

	[Fact]
	public async Task DeleteConfirmationModal_ConfirmButton_InvokesCallback()
	{
		// Arrange
		var confirmed = false;

		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () =>
			{
				confirmed = true;
				return Task.CompletedTask;
			}))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Act
		var buttons = cut.FindAll("button");
		var confirmButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Delete"));
		confirmButton?.Click();

		// Assert
		confirmed.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteConfirmationModal_CancelButton_InvokesCallback()
	{
		// Arrange
		var cancelled = false;

		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () =>
			{
				cancelled = true;
				return Task.CompletedTask;
			})));

		// Act
		var buttons = cut.FindAll("button");
		var cancelButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Cancel"));
		cancelButton?.Click();

		// Assert
		cancelled.Should().BeTrue();
	}

	[Fact]
	public void DeleteConfirmationModal_WhenDeleting_ConfirmButtonDisabled()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.IsDeleting, true)
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert - When IsDeleting=true, button shows "Deleting..." and is disabled
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Deleting"));
		confirmButton.Should().NotBeNull();
		confirmButton!.HasAttribute("disabled").Should().BeTrue();
	}

	[Fact]
	public void DeleteConfirmationModal_WhenDeleting_ShowsLoadingSpinner()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.IsDeleting, true)
			.Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => { }))
			.Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => { })));

		// Assert
		cut.Markup.Should().Contain("animate-spin");
	}
}

/// <summary>
///   Tests for the DateRangePicker component.
/// </summary>
public class DateRangePickerTests : BunitTestBase
{
	[Fact]
	public void DateRangePicker_RendersPresetButtons()
	{
		// Arrange & Act
		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, _ => { })));

		// Assert
		cut.Markup.Should().Contain("Last 7 days");
		cut.Markup.Should().Contain("Last 30 days");
		cut.Markup.Should().Contain("Last 90 days");
		cut.Markup.Should().Contain("All time");
	}

	[Fact]
	public void DateRangePicker_RendersDateInputs()
	{
		// Arrange & Act
		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, _ => { })));

		// Assert
		cut.FindAll("input[type=\"date\"]").Should().HaveCount(2);
	}

	[Fact]
	public void DateRangePicker_WithStartDate_RendersStartDate()
	{
		// Arrange
		var startDate = new DateTime(2025, 1, 1);

		// Act
		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.StartDate, startDate)
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, _ => { })));

		// Assert
		cut.FindAll("input[type=\"date\"]")[0].GetAttribute("value").Should().Be("2025-01-01");
	}

	[Fact]
	public void DateRangePicker_WithEndDate_RendersEndDate()
	{
		// Arrange
		var endDate = new DateTime(2025, 1, 31);

		// Act
		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.EndDate, endDate)
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, _ => { })));

		// Assert
		cut.FindAll("input[type=\"date\"]")[1].GetAttribute("value").Should().Be("2025-01-31");
	}

	[Fact]
	public async Task DateRangePicker_PresetButton_UpdatesDates()
	{
		// Arrange
		var rangeChanged = false;
		var newStartDate = DateTime.MinValue;

		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, range =>
			{
				rangeChanged = true;
				newStartDate = range.Item1 ?? DateTime.MinValue;
				return Task.CompletedTask;
			})));

		// Act
		var buttons = cut.FindAll("button");
		var last7DaysButton = buttons.FirstOrDefault(b => b.TextContent.Contains("Last 7 days"));
		last7DaysButton?.Click();

		// Assert
		rangeChanged.Should().BeTrue();
		var expectedDate = DateTime.UtcNow.Date.AddDays(-7);
		newStartDate.Date.Should().Be(expectedDate);
	}

	[Fact]
	public void DateRangePicker_FromLabel_Displayed()
	{
		// Arrange & Act
		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, _ => { })));

		// Assert
		cut.Markup.Should().Contain("From");
	}

	[Fact]
	public void DateRangePicker_ToLabel_Displayed()
	{
		// Arrange & Act
		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, _ => { })));

		// Assert
		cut.Markup.Should().Contain("To");
	}

	[Fact]
	public async Task DateRangePicker_ManualDateChange_InvokesCallback()
	{
		// Arrange
		var rangeChanged = false;

		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, _ =>
			{
				rangeChanged = true;
				return Task.CompletedTask;
			})));

		// Act
		var inputs = cut.FindAll("input[type=\"date\"]");
		inputs[0].Change("2025-01-15");

		// Assert
		rangeChanged.Should().BeTrue();
	}

	[Fact]
	public void DateRangePicker_AllTimeButton_ClearsPreset()
	{
		// Arrange & Act
		var cut = Render<DateRangePicker>(parameters => parameters
			.Add(p => p.OnRangeChanged, EventCallback.Factory.Create<(DateTime?, DateTime?)>(this, _ => { })));

		// Assert
		var buttons = cut.FindAll("button");
		var allTimeButton = buttons.FirstOrDefault(b => b.TextContent.Contains("All time"));
		allTimeButton.Should().NotBeNull();
	}
}
