// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IssueComponent.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace Web.Components.Shared;

public partial class IssueComponent
{
	private global::Shared.Models.Issue? _archivingIssue;

	[Parameter] public global::Shared.Models.Issue Item { get; set; } = new();

	[Parameter] public global::Shared.Models.User LoggedInUser { get; set; } = new();

	/// <summary>
	///   GetIssueCategoryCssClass
	/// </summary>
	/// <param name="issue">Issue</param>
	/// <returns>string css class</returns>
	private static string GetIssueCategoryCssClass(global::Shared.Models.Issue issue)
	{
		string output = issue.Category.CategoryName switch
		{
			"Design" => "issue-entry-category-design",
			"Documentation" => "issue-entry-category-documentation",
			"Implementation" => "issue-entry-category-implementation",
			"Clarification" => "issue-entry-category-clarification",
			"Miscellaneous" => "issue-entry-category-miscellaneous",
			_ => "issue-entry-category-none"
		};

		return output;
	}

	/// <summary>
	///   GetIssueStatusCssClass method
	/// </summary>
	/// <param name="issue">Issue</param>
	/// <returns>string css class</returns>
	private static string GetIssueStatusCssClass(global::Shared.Models.Issue issue)
	{
		string output = issue.IssueStatus.StatusName switch
		{
			"Answered" => "issue-entry-status-answered",
			"InWork" => "issue-entry-status-inwork",
			"Watching" => "issue-entry-status-watching",
			"Dismissed" => "issue-entry-status-dismissed",
			_ => "issue-entry-status-none"
		};

		return output;
	}

	/// <summary>
	///   OpenDetailsPage method
	/// </summary>
	/// <param name="issue">Issue</param>
	private void OpenDetailsPage(global::Shared.Models.Issue issue)
	{
		NavManager.NavigateTo($"/Details/{issue.Id}");
	}

	/// <summary>
	///   Archive issue method
	/// </summary>
	private async Task ArchiveIssue()
	{
		_archivingIssue!.ArchivedBy = new UserDto(LoggedInUser);
		_archivingIssue!.Archived = true;
		await IssueService.UpdateIssue(_archivingIssue);
		_archivingIssue = null;
	}
}
