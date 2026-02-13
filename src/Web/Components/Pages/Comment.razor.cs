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
/// <seealso cref="Microsoft.AspNetCore.Mvc.RazorPages.PageModel" />
public partial class Comment
{
	private CreateCommentDto _comment = new();

	private IssueModel? _issue;

	private UserModel? _loggedInUser;

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
		CommentModel comment = new()
		{
			Issue = new BasicIssueModel(_issue!),
			Author = new BasicUserModel(_loggedInUser!),
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
	/// <param name="issue">IssueModel</param>
	private void OpenCommentForm(IssueModel issue)
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