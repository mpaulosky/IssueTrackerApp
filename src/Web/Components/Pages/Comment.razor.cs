// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Comment.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace IssueTracker.UI.Pages;

/// <summary>
///   Comment page class.
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Components.ComponentBase" />
public partial class Comment : ComponentBase
{
	[Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private IIssueService IssueService { get; set; } = default!;
	[Inject] private ICommentService CommentService { get; set; } = default!;
	[Inject] private IUserService UserService { get; set; } = default!;

	private CreateCommentDto _comment = new();

	private global::Shared.Models.Issue? _issue;

	private global::Shared.Models.User? _loggedInUser;

	[Parameter] public string? Id { get; set; }

	/// <summary>
	///   OnInitializedAsync event.
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_loggedInUser = await AuthProvider.GetUserFromAuth(UserService);

		_issue = await IssueService.GetIssue(Id);
	}

	/// <summary>
	///   CreateComment method.
	/// </summary>
	private async Task CreateComment()
	{
		Shared.Models.Comment comment = new()
		{
			Issue = new IssueDto(_issue!),
			Author = new UserDto(_loggedInUser!),
			Title = _comment.Title!,
			Description = _comment.Description!
		};

		await CommentService.CreateComment(comment);

		_comment = new CreateCommentDto();

		ClosePage();
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
	///   ClosePage method.
	/// </summary>
	private void ClosePage()
	{
		NavManager.NavigateTo("/");
	}
}
