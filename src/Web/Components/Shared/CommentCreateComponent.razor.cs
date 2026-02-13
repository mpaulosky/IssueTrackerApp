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
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private ICommentService CommentService { get; set; } = default!;

	private CreateCommentDto _comment = new();

	[Parameter] public global::Shared.Models.Issue Issue { get; set; } = new();

	[Parameter] public global::Shared.Models.User LoggedInUser { get; set; } = new();

	private async Task CreateComment()
	{
		global::Shared.Models.Comment comment = new()
		{
			Issue = new IssueDto(Issue),
			Author = new UserDto(LoggedInUser),
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
