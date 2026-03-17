// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkActionToolbarTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Components.Issues;

/// <summary>
///   Tests for BulkActionToolbar component.
/// </summary>
public class BulkActionToolbarTests : BunitTestBase
{
	private BulkSelectionState GetSelectionState() =>
		Services.GetRequiredService<BulkSelectionState>();

	#region Render Tests

	[Fact]
	public void BulkActionToolbar_WithNoSelection_RendersNothing()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();

		// Act
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().BeEmpty();
	}

	[Fact]
	public void BulkActionToolbar_WithSelection_RendersToolbar()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		// Act
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().NotBeEmpty();
		cut.Find("div").Should().NotBeNull();
	}

	[Fact]
	public void BulkActionToolbar_WithSingleSelection_DisplaysCorrectCount()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		// Act
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("1");
		cut.Markup.Should().Contain("issue selected");
	}

	[Fact]
	public void BulkActionToolbar_WithMultipleSelections_DisplaysCorrectCount()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");
		selectionState.SelectIssue("issue-2");
		selectionState.SelectIssue("issue-3");

		// Act
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("3");
		cut.Markup.Should().Contain("issues selected");
	}

	#endregion

	#region Admin Tests

	[Fact]
	public void BulkActionToolbar_WhenNotAdmin_HidesDeleteButton()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		// Act
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().NotContain("Delete");
	}

	[Fact]
	public void BulkActionToolbar_WhenAdmin_ShowsDeleteButton()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		// Act
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, true)
		);

		// Assert
		cut.Markup.Should().Contain("Delete");
	}

	#endregion

	#region Clear Selection Tests

	[Fact]
	public void BulkActionToolbar_ClickClearSelection_ClearsAllSelections()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");
		selectionState.SelectIssue("issue-2");

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Act
		var clearButton = cut.FindAll("button").First(b => b.TextContent.Contains("Clear selection"));
		clearButton.Click();

		// Assert
		selectionState.HasSelection.Should().BeFalse();
		selectionState.SelectedCount.Should().Be(0);
	}

	#endregion

	#region Status Dropdown Tests

	[Fact]
	public void BulkActionToolbar_ClickStatusButton_OpensDropdown()
	{
		// Arrange
		var statuses = new List<StatusDto>
		{
			CreateTestStatus(name: "Open"),
			CreateTestStatus(name: "In Progress"),
			CreateTestStatus(name: "Closed")
		};
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, statuses)
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Act
		var statusButton = cut.FindAll("button").First(b => b.TextContent.Contains("Status"));
		statusButton.Click();

		// Assert
		cut.Markup.Should().Contain("Open");
		cut.Markup.Should().Contain("In Progress");
		cut.Markup.Should().Contain("Closed");
	}

	[Fact]
	public async Task BulkActionToolbar_SelectStatus_InvokesCallback()
	{
		// Arrange
		var statuses = new List<StatusDto>
		{
			CreateTestStatus(name: "Open"),
			CreateTestStatus(name: "Closed")
		};
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		StatusDto? selectedStatus = null;
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, statuses)
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnChangeStatus, EventCallback.Factory.Create<StatusDto>(this, s => selectedStatus = s))
		);

		// Act - Open dropdown
		var statusButton = cut.FindAll("button").First(b => b.TextContent.Contains("Status"));
		statusButton.Click();

		// Act - Select status
		var openStatusButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Open");
		await cut.InvokeAsync(() => openStatusButton.Click());

		// Assert
		selectedStatus.Should().NotBeNull();
		selectedStatus!.StatusName.Should().Be("Open");
	}

	#endregion

	#region Category Dropdown Tests

	[Fact]
	public void BulkActionToolbar_ClickCategoryButton_OpensDropdown()
	{
		// Arrange
		var categories = new List<CategoryDto>
		{
			CreateTestCategory(name: "Bug"),
			CreateTestCategory(name: "Feature"),
			CreateTestCategory(name: "Enhancement")
		};
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, categories)
			.Add(p => p.IsAdmin, false)
		);

		// Act
		var categoryButton = cut.FindAll("button").First(b => b.TextContent.Contains("Category"));
		categoryButton.Click();

		// Assert
		cut.Markup.Should().Contain("Bug");
		cut.Markup.Should().Contain("Feature");
		cut.Markup.Should().Contain("Enhancement");
	}

	[Fact]
	public async Task BulkActionToolbar_SelectCategory_InvokesCallback()
	{
		// Arrange
		var categories = new List<CategoryDto>
		{
			CreateTestCategory(name: "Bug"),
			CreateTestCategory(name: "Feature")
		};
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		CategoryDto? selectedCategory = null;
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, categories)
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnChangeCategory, EventCallback.Factory.Create<CategoryDto>(this, c => selectedCategory = c))
		);

		// Act - Open dropdown
		var categoryButton = cut.FindAll("button").First(b => b.TextContent.Contains("Category"));
		categoryButton.Click();

		// Act - Select category
		var bugCategoryButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Bug");
		await cut.InvokeAsync(() => bugCategoryButton.Click());

		// Assert
		selectedCategory.Should().NotBeNull();
		selectedCategory!.CategoryName.Should().Be("Bug");
	}

	#endregion

	#region Export Tests

	[Fact]
	public async Task BulkActionToolbar_ClickExport_InvokesCallback()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		var exportInvoked = false;
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnExport, EventCallback.Factory.Create(this, () => exportInvoked = true))
		);

		// Act
		var exportButton = cut.FindAll("button").First(b => b.TextContent.Contains("Export"));
		await cut.InvokeAsync(() => exportButton.Click());

		// Assert
		exportInvoked.Should().BeTrue();
	}

	#endregion

	#region Delete Tests

	[Fact]
	public async Task BulkActionToolbar_ClickDelete_InvokesCallback()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		var deleteInvoked = false;
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, true)
			.Add(p => p.OnDelete, EventCallback.Factory.Create(this, () => deleteInvoked = true))
		);

		// Act
		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		await cut.InvokeAsync(() => deleteButton.Click());

		// Assert
		deleteInvoked.Should().BeTrue();
	}

	#endregion

	#region Selection State Changed Tests

	[Fact]
	public void BulkActionToolbar_WhenSelectionChanges_UpdatesDisplay()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Act - Add more selections
		selectionState.SelectIssue("issue-2");
		cut.Render();

		// Assert
		cut.Markup.Should().Contain("2");
		cut.Markup.Should().Contain("issues selected");
	}

	[Fact]
	public void BulkActionToolbar_WhenAllDeselected_HidesToolbar()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Assert toolbar is visible
		cut.Markup.Should().NotBeEmpty();

		// Act - Clear all selections
		selectionState.ClearSelection();
		cut.Render();

		// Assert toolbar is hidden
		cut.Markup.Should().BeEmpty();
	}

	#endregion

	#region Dropdown Toggle Tests

	[Fact]
	public void BulkActionToolbar_ToggleStatusDropdown_ClosesCategory()
	{
		// Arrange
		var statuses = new List<StatusDto> { CreateTestStatus(name: "Open") };
		var categories = new List<CategoryDto> { CreateTestCategory(name: "Bug") };
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, statuses)
			.Add(p => p.Categories, categories)
			.Add(p => p.IsAdmin, false)
		);

		// Act - Open category dropdown
		var categoryButton = cut.FindAll("button").First(b => b.TextContent.Contains("Category"));
		categoryButton.Click();

		// Assert category dropdown is open
		cut.Markup.Should().Contain("Bug");

		// Act - Open status dropdown
		var statusButton = cut.FindAll("button").First(b => b.TextContent.Contains("Status"));
		statusButton.Click();

		// Assert status dropdown is open and category is closed
		cut.Markup.Should().Contain("Open");
		// The category "Bug" should only appear once in the dropdown button, not in a dropdown menu
		var bugOccurrences = cut.FindAll("button").Count(b => b.TextContent.Trim() == "Bug");
		bugOccurrences.Should().Be(0); // Dropdown item should be closed
	}

	#endregion

	#region Dispose Tests

	[Fact]
	public void BulkActionToolbar_Dispose_UnsubscribesFromEvents()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.SelectIssue("issue-1");

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, Array.Empty<StatusDto>())
			.Add(p => p.Categories, Array.Empty<CategoryDto>())
			.Add(p => p.IsAdmin, false)
		);

		// Act
		cut.Dispose();

		// Assert - Should not throw when selection changes after dispose
		var exception = Record.Exception(() => selectionState.SelectIssue("issue-2"));
		exception.Should().BeNull();
	}

	#endregion
}
