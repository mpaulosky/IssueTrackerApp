// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     SetStatusComponent.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace Web.Components.Shared;

public partial class SetStatusComponent : ComponentBase
{
	private string? _settingStatus;
	private List<StatusModel> _statuses = new();

	[Parameter] public IssueModel Issue { get; set; } = new();

	[Parameter] public EventCallback<IssueModel> IssueChanged { get; set; }

	/// <summary>
	///   OnInitializedAsync method
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_statuses = await StatusService.GetStatuses();
	}

	/// <summary>
	///   CompleteSetStatus method
	/// </summary>
	private Task CompleteSetStatus()
	{
		Issue.IssueStatus = _settingStatus switch
		{
			"answered" => new BasicStatusModel(_statuses.First(s =>
				string.Equals(s.StatusName, _settingStatus, StringComparison.CurrentCultureIgnoreCase))),
			"inwork" => new BasicStatusModel(_statuses.First(s =>
				string.Equals(s.StatusName, _settingStatus, StringComparison.CurrentCultureIgnoreCase))),
			"watching" => new BasicStatusModel(_statuses.First(s =>
				string.Equals(s.StatusName, _settingStatus, StringComparison.CurrentCultureIgnoreCase))),
			"dismissed" => new BasicStatusModel(_statuses.First(s =>
				string.Equals(s.StatusName, _settingStatus, StringComparison.CurrentCultureIgnoreCase))),
			_ => Issue.IssueStatus
		};

		_settingStatus = null;

		SaveStatus();

		return IssueChanged.InvokeAsync(Issue);
	}

	private async void SaveStatus()
	{
		await IssueService.UpdateIssue(Issue);
	}
}