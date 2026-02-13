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
	[Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;
	[Inject] private IIssueService IssueService { get; set; } = default!;
	[Inject] private IStatusService StatusService { get; set; } = default!;

	private string? _settingStatus;
	private List<global::Shared.Models.Status> _statuses = new();

	[Parameter] public global::Shared.Models.Issue Issue { get; set; } = new();

	[Parameter] public EventCallback<global::Shared.Models.Issue> IssueChanged { get; set; }

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
			"answered" => new StatusDto(_statuses.First(s =>
				string.Equals(s.StatusName, _settingStatus, StringComparison.CurrentCultureIgnoreCase))),
			"inwork" => new StatusDto(_statuses.First(s =>
				string.Equals(s.StatusName, _settingStatus, StringComparison.CurrentCultureIgnoreCase))),
			"watching" => new StatusDto(_statuses.First(s =>
				string.Equals(s.StatusName, _settingStatus, StringComparison.CurrentCultureIgnoreCase))),
			"dismissed" => new StatusDto(_statuses.First(s =>
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
