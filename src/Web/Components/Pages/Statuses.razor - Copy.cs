// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Statuses.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

using Radzen.Blazor;

namespace IssueTracker.UI.Pages;

/// <summary>
///   Statuses partial class
/// </summary>
[UsedImplicitly]
public partial class Statuses
{
	private List<StatusModel>? _statuses = new();

	private RadzenDataGrid<StatusModel> _statusesGrid = new();
	private StatusModel? _statusToInsert;
	private StatusModel? _statusToUpdate;

	/// <summary>
	///   OnInitializedAsync event.
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_statuses = await StatusService.GetStatuses();
	}

	private async Task EditRow(StatusModel status)
	{
		_statusToUpdate = status;

		await _statusesGrid.EditRow(_statusToUpdate);
	}

	private async void OnUpdateRow(StatusModel status)
	{
		_statusToUpdate = null;

		await StatusService.UpdateStatus(status);
	}

	private async Task SaveRow(StatusModel status)
	{
		await _statusesGrid.UpdateRow(status);
	}

	private void CancelEdit(StatusModel status)
	{
		if (status == _statusToInsert)
		{
			_statusToInsert = null;
		}

		if (status == _statusToUpdate)
		{
			_statusToUpdate = null;
		}

		_statusesGrid.CancelEditRow(status);
	}

	private async Task DeleteRow(StatusModel status)
	{
		if (_statuses!.Contains(status))
		{
			_statuses.Remove(status);
		}

		_statusesGrid.CancelEditRow(status);

		await StatusService.ArchiveStatus(status);

		await _statusesGrid.Reload();
	}

	private async Task InsertRow()
	{
		_statusToInsert = new StatusModel();

		await _statusesGrid.InsertRow(_statusToInsert);
	}

	private async void OnCreateRow(StatusModel status)
	{
		if (status == _statusToInsert)
		{
			_statusToInsert = null;
		}

		await StatusService.CreateStatus(status);

		_statuses!.Add(status);

		await _statusesGrid.Reload();
	}

	/// <summary>
	///   ClosePage method.
	/// </summary>
	private void ClosePage()
	{
		NavManager.NavigateTo("/");
	}
}