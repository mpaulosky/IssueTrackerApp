// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Admin.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace IssueTracker.UI.Pages;

/// <summary>
///   Admin page class
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Mvc.RazorPages.PageModel" />
[UsedImplicitly]
public partial class Admin
{
	private string _currentEditingDescription = "";
	private string _currentEditingTitle = "";
	private string _editedDescription = "";
	private string _editedTitle = "";
	private List<IssueModel>? _issues;

	/// <summary>
	///   OnInitializedAsync event
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_issues = await IssueService.GetIssuesWaitingForApproval();
	}

	/// <summary>
	///   ApproveIssue method
	/// </summary>
	/// <param name="issue">IssueModel</param>
	private async Task ApproveIssue(IssueModel issue)
	{
		issue.ApprovedForRelease = true;

		_issues?.Remove(issue);

		await IssueService.UpdateIssue(issue);
	}

	/// <summary>
	///   RejectIssue method
	/// </summary>
	/// <param name="issue">IssueModel</param>
	private async Task RejectIssue(IssueModel issue)
	{
		issue.Rejected = true;

		_issues?.Remove(issue);

		await IssueService.UpdateIssue(issue);
	}

	/// <summary>
	///   EditTitle method
	/// </summary>
	/// <param name="model">IssueModel</param>
	private void EditTitle(IssueModel model)
	{
		_editedTitle = model.Title;
		_currentEditingTitle = model.Id;
		_currentEditingDescription = "";
	}

	/// <summary>
	///   SaveTitle method
	/// </summary>
	/// <param name="model">IssueModel</param>
	private async Task SaveTitle(IssueModel model)
	{
		_currentEditingTitle = string.Empty;
		model.Title = _editedTitle;
		await IssueService.UpdateIssue(model);
	}

	/// <summary>
	///   EditDescription method
	/// </summary>
	/// <param name="model">IssueModel</param>
	private void EditDescription(IssueModel model)
	{
		_editedDescription = model.Description;
		_currentEditingTitle = "";
		_currentEditingDescription = model.Id;
	}

	/// <summary>
	///   SaveDescription method
	/// </summary>
	/// <param name="model">IssueModel</param>
	private async Task SaveDescription(IssueModel model)
	{
		_currentEditingDescription = string.Empty;
		model.Description = _editedDescription;
		await IssueService.UpdateIssue(model);
	}

	/// <summary>
	///   ClosePage method
	/// </summary>
	private void ClosePage()
	{
		NavManager.NavigateTo("/");
	}
}