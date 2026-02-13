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
/// <seealso cref="Microsoft.AspNetCore.Components.ComponentBase" />
[UsedImplicitly]
public partial class Profile : ComponentBase
{
	[Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private IUserService UserService { get; set; } = default!;
	[Inject] private ICommentService CommentService { get; set; } = default!;
	[Inject] private IIssueService IssueService { get; set; } = default!;

	private List<global::Shared.Models.Issue>? _approved;
	private List<global::Shared.Models.Issue>? _archived;
	private List<Shared.Models.Comment>? _comments;
	private List<global::Shared.Models.Issue>? _issues;

	private global::Shared.Models.User? _loggedInUser;
	private List<global::Shared.Models.Issue>? _pending;
	private List<global::Shared.Models.Issue>? _rejected;

	/// <summary>
	///   OnInitializedAsync event
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_loggedInUser = await AuthProvider.GetUserFromAuth(UserService);

		_comments = await CommentService.GetCommentsByUser(_loggedInUser!.Id);

		List<global::Shared.Models.Issue> results = await IssueService.GetIssuesByUser(_loggedInUser.Id);

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
