// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkSelectionState.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

namespace Web.Services;

/// <summary>
/// Service for managing bulk selection state of issues across components.
/// </summary>
public sealed class BulkSelectionState
{
	private readonly HashSet<string> _selectedIssueIds = [];

	/// <summary>
	/// Event fired when selection state changes.
	/// </summary>
	public event Action? OnSelectionChanged;

	/// <summary>
	/// Gets the current selected issue IDs as a read-only collection.
	/// </summary>
	public IReadOnlySet<string> SelectedIssueIds => _selectedIssueIds;

	/// <summary>
	/// Gets the count of selected issues.
	/// </summary>
	public int SelectedCount => _selectedIssueIds.Count;

	/// <summary>
	/// Gets whether any issues are selected.
	/// </summary>
	public bool HasSelection => _selectedIssueIds.Count > 0;

	/// <summary>
	/// Selects a single issue by ID.
	/// </summary>
	/// <param name="id">The issue ID to select.</param>
	public void SelectIssue(string id)
	{
		if (_selectedIssueIds.Add(id))
		{
			OnSelectionChanged?.Invoke();
		}
	}

	/// <summary>
	/// Deselects a single issue by ID.
	/// </summary>
	/// <param name="id">The issue ID to deselect.</param>
	public void DeselectIssue(string id)
	{
		if (_selectedIssueIds.Remove(id))
		{
			OnSelectionChanged?.Invoke();
		}
	}

	/// <summary>
	/// Toggles the selection state of an issue.
	/// </summary>
	/// <param name="id">The issue ID to toggle.</param>
	public void ToggleIssue(string id)
	{
		if (_selectedIssueIds.Contains(id))
		{
			_selectedIssueIds.Remove(id);
		}
		else
		{
			_selectedIssueIds.Add(id);
		}
		OnSelectionChanged?.Invoke();
	}

	/// <summary>
	/// Selects all provided issue IDs.
	/// </summary>
	/// <param name="ids">The collection of issue IDs to select.</param>
	public void SelectAll(IEnumerable<string> ids)
	{
		var idsToAdd = ids.ToList();
		foreach (var id in idsToAdd)
		{
			_selectedIssueIds.Add(id);
		}
		OnSelectionChanged?.Invoke();
	}

	/// <summary>
	/// Clears all selections.
	/// </summary>
	public void ClearSelection()
	{
		if (_selectedIssueIds.Count > 0)
		{
			_selectedIssueIds.Clear();
			OnSelectionChanged?.Invoke();
		}
	}

	/// <summary>
	/// Checks if an issue is currently selected.
	/// </summary>
	/// <param name="id">The issue ID to check.</param>
	/// <returns>True if the issue is selected, false otherwise.</returns>
	public bool IsSelected(string id)
	{
		return _selectedIssueIds.Contains(id);
	}

	/// <summary>
	/// Checks if all provided IDs are selected.
	/// </summary>
	/// <param name="ids">The collection of issue IDs to check.</param>
	/// <returns>True if all IDs are selected, false otherwise.</returns>
	public bool AreAllSelected(IEnumerable<string> ids)
	{
		var idList = ids.ToList();
		return idList.Count > 0 && idList.All(id => _selectedIssueIds.Contains(id));
	}
}
