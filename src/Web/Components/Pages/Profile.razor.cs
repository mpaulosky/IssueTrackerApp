// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Profile.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace IssueTracker.UI.Pages;

/// <summary>
///   Profile page class
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Mvc.RazorPages.PageModel" />
[UsedImplicitly]
public partial class Profile
{
	private List<IssueModel>? _approved;
	private List<IssueModel>? _archived;
	private List<CommentModel>? _comments;
	private List<IssueModel>? _issues;

	private UserModel? _loggedInUser;
	private List<IssueModel>? _pending;
	private List<IssueModel>? _rejected;

	/// <summary>
	///   OnInitializedAsync event
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_loggedInUser = await AuthProvider.GetUserFromAuth(UserService);

		_comments = await CommentService.GetCommentsByUser(_loggedInUser!.Id);

		List<IssueModel> results = await IssueService.GetIssuesByUser(_loggedInUser.Id);

		if (results.Count != 0)
		{
			_issues = results.OrderByDescending(s => s.DateCreated).ToList();

			_approved = _issues
				.Where(s => s is { ApprovedForRelease: true, Archived: false, Rejected: false })
				.ToList();

			_archived = _issues
				.Where(s => s is { Archived: true, Rejected: false })
				.ToList();

			_pending = _issues
				.Where(s => s is { ApprovedForRelease: false, Rejected: false })
				.ToList();

			_rejected = _issues.Where(s => s.Rejected).ToList();
		}
	}

	/// <summary>
	///   ClosePage method
	/// </summary>
	private void ClosePage()
	{
		NavManager.NavigateTo("/");
	}
}