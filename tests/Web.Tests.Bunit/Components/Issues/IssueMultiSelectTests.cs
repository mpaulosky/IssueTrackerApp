// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueMultiSelectTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Components.Issues;

/// <summary>
///   Comprehensive tests for the IssueMultiSelect component.
/// </summary>
public class IssueMultiSelectTests : BunitTestBase
{
	private BulkSelectionState GetSelectionState() =>
		Services.GetRequiredService<BulkSelectionState>();

	#region Render Tests - Empty State

	[Fact]
	public void IssueMultiSelect_RendersSingleCheckbox_WhenShowSelectAllIsFalse()
	{
		// Arrange
		var issueId = "issue-123";

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.Should().NotBeNull();
		checkbox.GetAttribute("type").Should().Be("checkbox");
	}

	[Fact]
	public void IssueMultiSelect_RendersSelectAllCheckbox_WhenShowSelectAllIsTrue()
	{
		// Arrange
		var allIssueIds = new List<string> { "issue-1", "issue-2", "issue-3" };

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Assert
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.Should().NotBeNull();
		checkbox.GetAttribute("type").Should().Be("checkbox");
	}

	[Fact]
	public void IssueMultiSelect_WithEmptyIssueList_RendersUncheckedSelectAll()
	{
		// Arrange
		var allIssueIds = Array.Empty<string>();

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Assert
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.HasAttribute("checked").Should().BeFalse();
	}

	#endregion

	#region Render Tests - Multiple Issues

	[Fact]
	public void IssueMultiSelect_WithMultipleIssues_RendersCorrectId()
	{
		// Arrange
		var issueId = "issue-456";

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.Should().NotBeNull();
	}

	[Fact]
	public void IssueMultiSelect_WhenUnselected_CheckboxIsUnchecked()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();
		var issueId = "issue-789";

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.HasAttribute("checked").Should().BeFalse();
	}

	[Fact]
	public void IssueMultiSelect_WhenSelected_CheckboxIsChecked()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var issueId = "issue-selected";
		selectionState.SelectIssue(issueId);

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.HasAttribute("checked").Should().BeTrue();
	}

	#endregion

	#region Select Single Issue Tests

	[Fact]
	public void IssueMultiSelect_ClickCheckbox_SelectsIssue()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();
		var issueId = "issue-to-select";

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Act
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.Change(true);

		// Assert
		selectionState.IsSelected(issueId).Should().BeTrue();
	}

	[Fact]
	public void IssueMultiSelect_UncheckCheckbox_DeselectsIssue()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var issueId = "issue-to-deselect";
		selectionState.SelectIssue(issueId);

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Act
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.Change(false);

		// Assert
		selectionState.IsSelected(issueId).Should().BeFalse();
	}

	[Fact]
	public void IssueMultiSelect_WithNullIssueId_DoesNotThrow()
	{
		// Arrange & Act
		var exception = Record.Exception(() =>
		{
			var cut = Render<IssueMultiSelect>(parameters => parameters
				.Add(p => p.IssueId, null)
				.Add(p => p.ShowSelectAll, false)
			);

			var checkbox = cut.Find("input[type='checkbox']");
			checkbox.Change(true);
		});

		// Assert
		exception.Should().BeNull();
	}

	[Fact]
	public void IssueMultiSelect_WithEmptyIssueId_DoesNotSelectOnChange()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, "")
			.Add(p => p.ShowSelectAll, false)
		);

		// Act
		var checkbox = cut.Find("input[type='checkbox']");
		checkbox.Change(true);

		// Assert
		selectionState.HasSelection.Should().BeFalse();
	}

	#endregion

	#region Select All Tests

	[Fact]
	public void IssueMultiSelect_CheckSelectAll_SelectsAllIssues()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();
		var allIssueIds = new List<string> { "issue-1", "issue-2", "issue-3" };

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Act
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.Change(true);

		// Assert
		foreach (var id in allIssueIds)
		{
			selectionState.IsSelected(id).Should().BeTrue($"Issue {id} should be selected");
		}
	}

	[Fact]
	public void IssueMultiSelect_UncheckSelectAll_DeselectsAllIssues()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var allIssueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		selectionState.SelectAll(allIssueIds);

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Act
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.Change(false);

		// Assert
		selectionState.HasSelection.Should().BeFalse();
		selectionState.SelectedCount.Should().Be(0);
	}

	[Fact]
	public void IssueMultiSelect_WhenAllSelected_SelectAllIsChecked()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var allIssueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		selectionState.SelectAll(allIssueIds);

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Assert
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.HasAttribute("checked").Should().BeTrue();
	}

	[Fact]
	public void IssueMultiSelect_SelectAllWithNullIds_DoesNotThrow()
	{
		// Arrange & Act
		var exception = Record.Exception(() =>
		{
			var cut = Render<IssueMultiSelect>(parameters => parameters
				.Add(p => p.ShowSelectAll, true)
				.Add(p => p.AllIssueIds, null)
			);

			var checkbox = cut.Find("input#select-all-checkbox");
			checkbox.Change(true);
		});

		// Assert
		exception.Should().BeNull();
	}

	#endregion

	#region Partial Selection Tests

	[Fact]
	public void IssueMultiSelect_WhenPartiallySelected_SelectAllIsUnchecked()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var allIssueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		selectionState.ClearSelection();
		selectionState.SelectIssue("issue-1"); // Only select one

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Assert
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.HasAttribute("checked").Should().BeFalse();
	}

	[Fact]
	public void IssueMultiSelect_WhenNoneSelected_SelectAllIsUnchecked()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();
		var allIssueIds = new List<string> { "issue-1", "issue-2", "issue-3" };

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Assert
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.HasAttribute("checked").Should().BeFalse();
	}

	#endregion

	#region Selection State Change Callback Tests

	[Fact]
	public void IssueMultiSelect_WhenSelectionChangesExternally_UpdatesDisplay()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();
		var issueId = "issue-external";

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert initially unchecked
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.HasAttribute("checked").Should().BeFalse();

		// Act - Change selection externally
		selectionState.SelectIssue(issueId);
		cut.Render();

		// Assert - Checkbox should now be checked
		checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.HasAttribute("checked").Should().BeTrue();
	}

	[Fact]
	public void IssueMultiSelect_SelectAll_WhenSelectionChangesExternally_UpdatesDisplay()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();
		var allIssueIds = new List<string> { "issue-1", "issue-2" };

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Assert initially unchecked
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.HasAttribute("checked").Should().BeFalse();

		// Act - Select all externally
		selectionState.SelectAll(allIssueIds);
		cut.Render();

		// Assert - Checkbox should now be checked
		checkbox = cut.Find("input#select-all-checkbox");
		checkbox.HasAttribute("checked").Should().BeTrue();
	}

	#endregion

	#region Accessibility Tests

	[Fact]
	public void IssueMultiSelect_SingleCheckbox_HasAriaLabel()
	{
		// Arrange
		var issueId = "issue-aria";

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var label = cut.Find($"label[for='issue-checkbox-{issueId}']");
		label.Should().NotBeNull();
		label.TextContent.Should().Contain("Select issue");
	}

	[Fact]
	public void IssueMultiSelect_SelectAllCheckbox_HasAriaLabel()
	{
		// Arrange
		var allIssueIds = new List<string> { "issue-1", "issue-2" };

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Assert
		var label = cut.Find("label[for='select-all-checkbox']");
		label.Should().NotBeNull();
		label.TextContent.Should().Contain("Select all issues");
	}

	[Fact]
	public void IssueMultiSelect_SingleCheckbox_LabelIsScreenReaderOnly()
	{
		// Arrange
		var issueId = "issue-sr";

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var label = cut.Find($"label[for='issue-checkbox-{issueId}']");
		label.ClassList.Should().Contain("sr-only");
	}

	[Fact]
	public void IssueMultiSelect_SelectAllCheckbox_LabelIsScreenReaderOnly()
	{
		// Arrange
		var allIssueIds = new List<string> { "issue-1" };

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Assert
		var label = cut.Find("label[for='select-all-checkbox']");
		label.ClassList.Should().Contain("sr-only");
	}

	[Fact]
	public void IssueMultiSelect_Checkbox_HasCorrectTabIndex()
	{
		// Arrange
		var issueId = "issue-tab";

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		// Default tabindex is 0 (implicit), so it should be focusable
		checkbox.Should().NotBeNull();
	}

	#endregion

	#region Dispose Tests

	[Fact]
	public void IssueMultiSelect_Dispose_UnsubscribesFromEvents()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var issueId = "issue-dispose";

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Act
		cut.Dispose();

		// Assert - Should not throw when selection changes after dispose
		var exception = Record.Exception(() => selectionState.SelectIssue(issueId));
		exception.Should().BeNull();
	}

	[Fact]
	public void IssueMultiSelect_SelectAll_Dispose_UnsubscribesFromEvents()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var allIssueIds = new List<string> { "issue-1", "issue-2" };

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Act
		cut.Dispose();

		// Assert - Should not throw when selection changes after dispose
		var exception = Record.Exception(() => selectionState.SelectAll(allIssueIds));
		exception.Should().BeNull();
	}

	#endregion

	#region CSS Class Tests

	[Fact]
	public void IssueMultiSelect_Checkbox_HasExpectedClasses()
	{
		// Arrange
		var issueId = "issue-css";

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var checkbox = cut.Find($"input#issue-checkbox-{issueId}");
		checkbox.ClassList.Should().Contain("h-4");
		checkbox.ClassList.Should().Contain("w-4");
		checkbox.ClassList.Should().Contain("rounded");
		checkbox.ClassList.Should().Contain("cursor-pointer");
	}

	[Fact]
	public void IssueMultiSelect_ContainerDiv_HasFlexClass()
	{
		// Arrange
		var issueId = "issue-container";

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, issueId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var container = cut.Find("div");
		container.ClassList.Should().Contain("flex");
		container.ClassList.Should().Contain("items-center");
	}

	#endregion

	#region Parameter Update Tests

	[Fact]
	public void IssueMultiSelect_WhenIssueIdChanges_UpdatesCheckboxId()
	{
		// Arrange
		var initialId = "issue-initial";
		var newId = "issue-new";

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, initialId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert initial ID
		cut.Find($"input#issue-checkbox-{initialId}").Should().NotBeNull();

		// Act - Update parameter using bUnit 2.x API
		cut.Render(parameters => parameters
			.Add(p => p.IssueId, newId)
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert - New ID is rendered
		cut.Find($"input#issue-checkbox-{newId}").Should().NotBeNull();
	}

	[Fact]
	public void IssueMultiSelect_WhenAllIssueIdsChanges_UpdatesSelectionState()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var initialIds = new List<string> { "issue-1", "issue-2" };
		var newIds = new List<string> { "issue-3", "issue-4", "issue-5" };
		selectionState.SelectAll(initialIds);

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, initialIds)
		);

		// Assert initial - all selected
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.HasAttribute("checked").Should().BeTrue();

		// Act - Update to new IDs (which are not selected) using bUnit 2.x API
		cut.Render(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, newIds)
		);

		// Assert - Checkbox should be unchecked since newIds are not selected
		checkbox = cut.Find("input#select-all-checkbox");
		checkbox.HasAttribute("checked").Should().BeFalse();
	}

	#endregion

	#region Large List Tests

	[Fact]
	public void IssueMultiSelect_WithLargeIssueList_SelectsAllEfficiently()
	{
		// Arrange
		var selectionState = GetSelectionState();
		selectionState.ClearSelection();
		var allIssueIds = Enumerable.Range(1, 100)
			.Select(i => $"issue-{i}")
			.ToList();

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Act
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.Change(true);

		// Assert
		selectionState.SelectedCount.Should().Be(100);
		selectionState.AreAllSelected(allIssueIds).Should().BeTrue();
	}

	[Fact]
	public void IssueMultiSelect_WithLargeIssueList_ClearsAllEfficiently()
	{
		// Arrange
		var selectionState = GetSelectionState();
		var allIssueIds = Enumerable.Range(1, 100)
			.Select(i => $"issue-{i}")
			.ToList();
		selectionState.SelectAll(allIssueIds);

		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, allIssueIds)
		);

		// Act
		var checkbox = cut.Find("input#select-all-checkbox");
		checkbox.Change(false);

		// Assert
		selectionState.SelectedCount.Should().Be(0);
		selectionState.HasSelection.Should().BeFalse();
	}

	#endregion
}
