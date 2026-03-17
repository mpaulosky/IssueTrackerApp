// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkSelectionStateTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BulkSelectionState"/>.
/// Tests cover selection state management, property behavior, and event notifications.
/// </summary>
public sealed class BulkSelectionStateTests
{
	private readonly BulkSelectionState _sut;

	public BulkSelectionStateTests()
	{
		_sut = new BulkSelectionState();
	}

	#region SelectIssue Tests

	[Fact]
	public void SelectIssue_AddsIssueToSelection()
	{
		// Arrange
		var issueId = "issue-1";

		// Act
		_sut.SelectIssue(issueId);

		// Assert
		_sut.SelectedIssueIds.Should().Contain(issueId);
		_sut.SelectedCount.Should().Be(1);
	}

	[Fact]
	public void SelectIssue_FiresOnSelectionChangedEvent()
	{
		// Arrange
		var issueId = "issue-1";
		var eventFired = false;
		_sut.OnSelectionChanged += () => eventFired = true;

		// Act
		_sut.SelectIssue(issueId);

		// Assert
		eventFired.Should().BeTrue();
	}

	[Fact]
	public void SelectIssue_WhenAlreadySelected_DoesNotFireEvent()
	{
		// Arrange
		var issueId = "issue-1";
		_sut.SelectIssue(issueId);

		var eventFiredCount = 0;
		_sut.OnSelectionChanged += () => eventFiredCount++;

		// Act
		_sut.SelectIssue(issueId);

		// Assert
		eventFiredCount.Should().Be(0);
		_sut.SelectedCount.Should().Be(1);
	}

	#endregion

	#region DeselectIssue Tests

	[Fact]
	public void DeselectIssue_RemovesIssueFromSelection()
	{
		// Arrange
		var issueId = "issue-1";
		_sut.SelectIssue(issueId);

		// Act
		_sut.DeselectIssue(issueId);

		// Assert
		_sut.SelectedIssueIds.Should().NotContain(issueId);
		_sut.SelectedCount.Should().Be(0);
	}

	[Fact]
	public void DeselectIssue_FiresOnSelectionChangedEvent()
	{
		// Arrange
		var issueId = "issue-1";
		_sut.SelectIssue(issueId);

		var eventFired = false;
		_sut.OnSelectionChanged += () => eventFired = true;

		// Act
		_sut.DeselectIssue(issueId);

		// Assert
		eventFired.Should().BeTrue();
	}

	[Fact]
	public void DeselectIssue_WhenNotSelected_DoesNotFireEvent()
	{
		// Arrange
		var issueId = "issue-1";
		var eventFiredCount = 0;
		_sut.OnSelectionChanged += () => eventFiredCount++;

		// Act
		_sut.DeselectIssue(issueId);

		// Assert
		eventFiredCount.Should().Be(0);
	}

	#endregion

	#region ToggleIssue Tests

	[Fact]
	public void ToggleIssue_WhenNotSelected_SelectsIssue()
	{
		// Arrange
		var issueId = "issue-1";

		// Act
		_sut.ToggleIssue(issueId);

		// Assert
		_sut.IsSelected(issueId).Should().BeTrue();
	}

	[Fact]
	public void ToggleIssue_WhenSelected_DeselectsIssue()
	{
		// Arrange
		var issueId = "issue-1";
		_sut.SelectIssue(issueId);

		// Act
		_sut.ToggleIssue(issueId);

		// Assert
		_sut.IsSelected(issueId).Should().BeFalse();
	}

	[Fact]
	public void ToggleIssue_AlwaysFiresOnSelectionChangedEvent()
	{
		// Arrange
		var issueId = "issue-1";
		var eventFiredCount = 0;
		_sut.OnSelectionChanged += () => eventFiredCount++;

		// Act
		_sut.ToggleIssue(issueId);
		_sut.ToggleIssue(issueId);

		// Assert
		eventFiredCount.Should().Be(2);
	}

	#endregion

	#region SelectAll Tests

	[Fact]
	public void SelectAll_SelectsAllProvidedIds()
	{
		// Arrange
		var issueIds = new[] { "issue-1", "issue-2", "issue-3" };

		// Act
		_sut.SelectAll(issueIds);

		// Assert
		_sut.SelectedIssueIds.Should().BeEquivalentTo(issueIds);
		_sut.SelectedCount.Should().Be(3);
	}

	[Fact]
	public void SelectAll_FiresOnSelectionChangedEvent()
	{
		// Arrange
		var issueIds = new[] { "issue-1", "issue-2" };
		var eventFired = false;
		_sut.OnSelectionChanged += () => eventFired = true;

		// Act
		_sut.SelectAll(issueIds);

		// Assert
		eventFired.Should().BeTrue();
	}

	[Fact]
	public void SelectAll_WithEmptyList_StillFiresEvent()
	{
		// Arrange
		var issueIds = Array.Empty<string>();
		var eventFired = false;
		_sut.OnSelectionChanged += () => eventFired = true;

		// Act
		_sut.SelectAll(issueIds);

		// Assert
		eventFired.Should().BeTrue();
	}

	#endregion

	#region ClearSelection Tests

	[Fact]
	public void ClearSelection_RemovesAllSelectedIssues()
	{
		// Arrange
		_sut.SelectIssue("issue-1");
		_sut.SelectIssue("issue-2");

		// Act
		_sut.ClearSelection();

		// Assert
		_sut.SelectedIssueIds.Should().BeEmpty();
		_sut.SelectedCount.Should().Be(0);
		_sut.HasSelection.Should().BeFalse();
	}

	[Fact]
	public void ClearSelection_FiresOnSelectionChangedEvent()
	{
		// Arrange
		_sut.SelectIssue("issue-1");
		var eventFired = false;
		_sut.OnSelectionChanged += () => eventFired = true;

		// Act
		_sut.ClearSelection();

		// Assert
		eventFired.Should().BeTrue();
	}

	[Fact]
	public void ClearSelection_WhenEmpty_DoesNotFireEvent()
	{
		// Arrange
		var eventFiredCount = 0;
		_sut.OnSelectionChanged += () => eventFiredCount++;

		// Act
		_sut.ClearSelection();

		// Assert
		eventFiredCount.Should().Be(0);
	}

	#endregion

	#region IsSelected Tests

	[Fact]
	public void IsSelected_WhenIssueIsSelected_ReturnsTrue()
	{
		// Arrange
		var issueId = "issue-1";
		_sut.SelectIssue(issueId);

		// Act
		var result = _sut.IsSelected(issueId);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void IsSelected_WhenIssueIsNotSelected_ReturnsFalse()
	{
		// Arrange
		var issueId = "issue-1";

		// Act
		var result = _sut.IsSelected(issueId);

		// Assert
		result.Should().BeFalse();
	}

	#endregion

	#region AreAllSelected Tests

	[Fact]
	public void AreAllSelected_WhenAllAreSelected_ReturnsTrue()
	{
		// Arrange
		var issueIds = new[] { "issue-1", "issue-2", "issue-3" };
		_sut.SelectAll(issueIds);

		// Act
		var result = _sut.AreAllSelected(issueIds);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void AreAllSelected_WhenPartiallySelected_ReturnsFalse()
	{
		// Arrange
		_sut.SelectIssue("issue-1");
		_sut.SelectIssue("issue-2");
		var idsToCheck = new[] { "issue-1", "issue-2", "issue-3" };

		// Act
		var result = _sut.AreAllSelected(idsToCheck);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void AreAllSelected_WithEmptyList_ReturnsFalse()
	{
		// Arrange
		_sut.SelectIssue("issue-1");
		var emptyList = Array.Empty<string>();

		// Act
		var result = _sut.AreAllSelected(emptyList);

		// Assert
		result.Should().BeFalse();
	}

	#endregion

	#region Property Tests

	[Fact]
	public void SelectedCount_ReturnsCorrectCount()
	{
		// Arrange & Act
		_sut.SelectIssue("issue-1");
		_sut.SelectIssue("issue-2");
		_sut.SelectIssue("issue-3");

		// Assert
		_sut.SelectedCount.Should().Be(3);
	}

	[Fact]
	public void HasSelection_WhenNoSelection_ReturnsFalse()
	{
		// Act & Assert
		_sut.HasSelection.Should().BeFalse();
	}

	[Fact]
	public void HasSelection_WhenHasSelection_ReturnsTrue()
	{
		// Arrange
		_sut.SelectIssue("issue-1");

		// Act & Assert
		_sut.HasSelection.Should().BeTrue();
	}

	[Fact]
	public void SelectedIssueIds_ReturnsReadOnlySet()
	{
		// Arrange
		_sut.SelectIssue("issue-1");

		// Act
		var selectedIds = _sut.SelectedIssueIds;

		// Assert
		selectedIds.Should().BeAssignableTo<IReadOnlySet<string>>();
		selectedIds.Should().Contain("issue-1");
	}

	#endregion
}
