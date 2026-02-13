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
public partial class Statuses : ComponentBase
{
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private IStatusService StatusService { get; set; } = default!;
	private List<global::Shared.Models.Status>? _statuses = new();

	private RadzenDataGrid<global::Shared.Models.Status> _statusesGrid = new();
	private global::Shared.Models.Status? _statusToInsert;
	private global::Shared.Models.Status? _statusToUpdate;

	/// <summary>
	///   OnInitializedAsync event.
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_statuses = await StatusService.GetStatuses();
	}

	private async Task EditRow(global::Shared.Models.Status status)
	{
		_statusToUpdate = status;

		await _statusesGrid.EditRow(_statusToUpdate);
	}

	private async void OnUpdateRow(global::Shared.Models.Status status)
	{
		_statusToUpdate = null;

		await StatusService.UpdateStatus(status);
	}

	private async Task SaveRow(global::Shared.Models.Status status)
	{
		await _statusesGrid.UpdateRow(status);
	}

	private void CancelEdit(global::Shared.Models.Status status)
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

	private async Task DeleteRow(global::Shared.Models.Status status)
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
		_statusToInsert = new global::Shared.Models.Status();

		await _statusesGrid.InsertRow(_statusToInsert);
	}

	private async void OnCreateRow(global::Shared.Models.Status status)
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
