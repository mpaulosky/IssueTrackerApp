// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CommentCreateComponent.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace Web.Components.Shared;

/// <summary>
///   Razor component for creating a comment.
/// </summary>
public partial class CommentCreateComponent
{
	private CreateCommentDto _comment = new();

	[Parameter] public IssueModel Issue { get; set; } = new();

	[Parameter] public UserModel LoggedInUser { get; set; } = new();

	private async Task CreateComment()
	{
		CommentModel comment = new()
		{
			Issue = new BasicIssueModel(Issue),
			Author = new BasicUserModel(LoggedInUser),
			Title = _comment.Title!,
			Description = _comment.Description!
		};

		await CommentService.CreateComment(comment);

		_comment = new CreateCommentDto();

		ClosePage();
	}

	/// <summary>
	///   ClosePage method.
	/// </summary>
	private void ClosePage()
	{
		NavManager.NavigateTo($"/Details/{Issue.Id}");
	}
}