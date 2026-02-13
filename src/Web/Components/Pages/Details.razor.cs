// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Details.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace IssueTracker.UI.Pages;

/// <summary>
///   Details class
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Components.ComponentBase" />
[UsedImplicitly]
public partial class Details : ComponentBase
{
	[Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private IIssueService IssueService { get; set; } = default!;
	[Inject] private IUserService UserService { get; set; } = default!;
	[Inject] private IStatusService StatusService { get; set; } = default!;
	[Inject] private ICommentService CommentService { get; set; } = default!;

	private List<Shared.Models.Comment>? _comments = new();

	private global::Shared.Models.Issue? _issue = new();

	private global::Shared.Models.User? _loggedInUser = new();

	[Parameter] public string? Id { get; set; }

	/// <summary>
	///   OnInitializedAsync method
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_loggedInUser = await AuthProvider.GetUserFromAuth(UserService);

		ArgumentNullException.ThrowIfNull(Id);

		_issue = await IssueService.GetIssue(Id);
		IssueDto issue = new(_issue);
		_comments = await CommentService.GetCommentsByIssue(issue);
		await StatusService.GetStatuses();
	}

	/// <summary>
	///   OpenCommentForm method
	/// </summary>
	/// <param name="issue">Issue</param>
	private void OpenCommentForm(global::Shared.Models.Issue issue)
	{
		if (_loggedInUser is not null)
		{
			NavManager.NavigateTo($"/Comment/{issue.Id}");
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
