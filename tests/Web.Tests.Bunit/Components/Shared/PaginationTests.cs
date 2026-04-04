// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     PaginationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Shared;

namespace Web.Tests.Bunit.Components.Shared;

/// <summary>
///   Tests for the Pagination component.
/// </summary>
public sealed class PaginationTests : BunitTestBase
{
	#region Visibility Guard Tests

	[Fact]
	public void Renders_Nothing_WhenTotalPagesIsOne()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 1)
			.Add(c => c.CurrentPage, 1)
			.Add(c => c.TotalItems, 5));

		// Assert — the @if (TotalPages > 1) guard means no markup at all
		cut.Markup.Trim().Should().BeEmpty();
	}

	[Fact]
	public void Renders_Nothing_WhenTotalPagesIsZero()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 0)
			.Add(c => c.CurrentPage, 1)
			.Add(c => c.TotalItems, 0));

		// Assert
		cut.Markup.Trim().Should().BeEmpty();
	}

	[Fact]
	public void Renders_Nav_WhenTotalPagesIsGreaterThanOne()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 3)
			.Add(c => c.CurrentPage, 1)
			.Add(c => c.TotalItems, 30));

		// Assert
		cut.Find("nav").Should().NotBeNull();
	}

	#endregion

	#region Previous / Next Button Tests

	[Fact]
	public void Previous_Button_NotRendered_WhenOnFirstPage()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 3)
			.Add(c => c.CurrentPage, 1)
			.Add(c => c.TotalItems, 30));

		// Assert — "Previous" text should not appear at all
		cut.Markup.Should().NotContain("Previous");
	}

	[Fact]
	public void Previous_Button_Rendered_WhenNotOnFirstPage()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 3)
			.Add(c => c.CurrentPage, 2)
			.Add(c => c.TotalItems, 30));

		// Assert
		cut.Markup.Should().Contain("Previous");
	}

	[Fact]
	public void Next_Button_NotRendered_WhenOnLastPage()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 3)
			.Add(c => c.CurrentPage, 3)
			.Add(c => c.TotalItems, 30));

		// Assert
		cut.Markup.Should().NotContain(">Next<");
		cut.FindAll("button").Select(b => b.TextContent).Should().NotContain(t => t.Contains("Next"));
	}

	[Fact]
	public void Next_Button_Rendered_WhenNotOnLastPage()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 3)
			.Add(c => c.CurrentPage, 1)
			.Add(c => c.TotalItems, 30));

		// Assert
		cut.Markup.Should().Contain("Next");
	}

	#endregion

	#region EventCallback Tests

	[Fact]
	public async Task Next_Click_FiresOnPageChange_WithNextPageNumber()
	{
		// Arrange
		var capturedPage = 0;
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 5)
			.Add(c => c.CurrentPage, 2)
			.Add(c => c.TotalItems, 50)
			.Add(c => c.OnPageChange, EventCallback.Factory.Create<int>(this, v => capturedPage = v)));

		// Act — find the button containing "Next" text
		var nextButton = cut.FindAll("button").First(b => b.TextContent.Contains("Next"));
		await cut.InvokeAsync(() => nextButton.Click());

		// Assert
		capturedPage.Should().Be(3);
	}

	[Fact]
	public async Task Previous_Click_FiresOnPageChange_WithPreviousPageNumber()
	{
		// Arrange
		var capturedPage = 0;
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 5)
			.Add(c => c.CurrentPage, 3)
			.Add(c => c.TotalItems, 50)
			.Add(c => c.OnPageChange, EventCallback.Factory.Create<int>(this, v => capturedPage = v)));

		// Act
		var prevButton = cut.FindAll("button").First(b => b.TextContent.Contains("Previous"));
		await cut.InvokeAsync(() => prevButton.Click());

		// Assert
		capturedPage.Should().Be(2);
	}

	[Fact]
	public async Task PageNumber_Click_FiresOnPageChange_WithCorrectPage()
	{
		// Arrange — TotalPages=3, CurrentPage=1 → GetPageNumbers returns [1,2,3] (no ellipsis),
		// so the "3" button is guaranteed to be present.
		var capturedPage = 0;
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 3)
			.Add(c => c.CurrentPage, 1)
			.Add(c => c.TotalItems, 30)
			.Add(c => c.OnPageChange, EventCallback.Factory.Create<int>(this, v => capturedPage = v)));

		// Act — click page "3" (not the current page, so guard passes and callback fires)
		var pageButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "3");
		await cut.InvokeAsync(() => pageButton.Click());

		// Assert
		capturedPage.Should().Be(3);
	}

	[Fact]
	public async Task CurrentPage_Click_DoesNotFireOnPageChange()
	{
		// Arrange — clicking the already-active page should be a no-op per OnPageClick guard
		var wasCalled = false;
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 5)
			.Add(c => c.CurrentPage, 2)
			.Add(c => c.TotalItems, 50)
			.Add(c => c.OnPageChange, EventCallback.Factory.Create<int>(this, _ => wasCalled = true)));

		// Act — click the current page "2"
		var currentPageButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "2");
		await cut.InvokeAsync(() => currentPageButton.Click());

		// Assert
		wasCalled.Should().BeFalse();
	}

	#endregion

	#region Active Page Styling

	[Fact]
	public void CurrentPage_Button_HasActiveBorderClass()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 5)
			.Add(c => c.CurrentPage, 2)
			.Add(c => c.TotalItems, 50));

		// Assert — active page button should have border-primary-500 per GetPageButtonClasses
		var currentButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "2");
		currentButton.GetAttribute("class").Should().Contain("border-primary-500");
	}

	[Fact]
	public void NonCurrentPage_Button_HasTransparentBorderClass()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 5)
			.Add(c => c.CurrentPage, 2)
			.Add(c => c.TotalItems, 50));

		// Assert — non-active page should have border-transparent
		var otherButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "3");
		otherButton.GetAttribute("class").Should().Contain("border-transparent");
	}

	#endregion

	#region Ellipsis and Status Text

	[Fact]
	public void Ellipsis_Rendered_WhenCurrentPageIsInMiddleOfLargePageRange()
	{
		// Arrange & Act — page 10 of 20 triggers showEllipsisStart and showEllipsisEnd
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 20)
			.Add(c => c.CurrentPage, 10)
			.Add(c => c.TotalItems, 200));

		// Assert — ellipsis spans contain "..." text
		var ellipsisSpans = cut.FindAll("span").Where(s => s.TextContent.Contains("...")).ToList();
		ellipsisSpans.Should().NotBeEmpty();
	}

	[Fact]
	public void NoEllipsis_WhenTotalPagesIsSmall()
	{
		// Arrange & Act — with only 4 pages, no ellipsis needed
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 4)
			.Add(c => c.CurrentPage, 2)
			.Add(c => c.TotalItems, 40));

		// Assert
		var ellipsisSpans = cut.FindAll("span").Where(s => s.TextContent.Contains("...")).ToList();
		ellipsisSpans.Should().BeEmpty();
	}

	[Fact]
	public void Ellipsis_NotShownAtStart_WhenCurrentPageIsExactlyFour()
	{
		// Arrange & Act — showEllipsisStart = (CurrentPage > 4) = false at page 4
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 20)
			.Add(c => c.CurrentPage, 4)
			.Add(c => c.TotalItems, 200));

		// Assert — page 4 is NOT > 4 so no start ellipsis; page 1 is adjacent to window
		var ellipsisSpans = cut.FindAll("span").Where(s => s.TextContent.Contains("...")).ToList();
		// There may be a trailing ellipsis but NOT a leading one — total should be exactly 1
		ellipsisSpans.Should().HaveCount(1);
	}

	[Fact]
	public void Ellipsis_ShownAtStart_WhenCurrentPageIsFive()
	{
		// Arrange & Act — showEllipsisStart = (5 > 4) = true at page 5
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 20)
			.Add(c => c.CurrentPage, 5)
			.Add(c => c.TotalItems, 200));

		// Assert — both start AND end ellipsis should appear → 2 ellipsis spans
		var ellipsisSpans = cut.FindAll("span").Where(s => s.TextContent.Contains("...")).ToList();
		ellipsisSpans.Should().HaveCount(2);
	}

	[Fact]
	public void StatusText_ShowsCurrentPageOfTotalAndTotalItems()
	{
		// Arrange & Act
		var cut = Render<Pagination>(p => p
			.Add(c => c.TotalPages, 5)
			.Add(c => c.CurrentPage, 2)
			.Add(c => c.TotalItems, 50));

		// Assert — "Showing page 2 of 5 (50 total items)"
		cut.Markup.Should().Contain("page 2 of 5");
		cut.Markup.Should().Contain("50 total items");
	}

	#endregion
}
