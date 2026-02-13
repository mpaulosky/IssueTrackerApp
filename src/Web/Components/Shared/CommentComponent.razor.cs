// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CommentComponent.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace Web.Components.Shared;

/// <summary>
///   Razor component for displaying a comment.
/// </summary>
public partial class CommentComponent
{
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;
	[Inject] private ICommentService CommentService { get; set; } = default!;

	private global::Shared.Models.Comment? _archivingComment;
	[Parameter] public global::Shared.Models.Comment Item { get; set; } = new();
	[Parameter] public global::Shared.Models.User LoggedInUser { get; set; } = new();

	/// <summary>
	///   Check if the logged in user is able to mark the comment as an answer.
	/// </summary>
	/// <returns>True if the user can mark the comment as an answer, otherwise false.</returns>
	private bool CanMarkAnswer()
	{
		ObjectId loggedInUserId = ObjectId.Empty;
		if (!string.IsNullOrEmpty(LoggedInUser?.Id))
		{
			loggedInUserId = ObjectId.Parse(LoggedInUser.Id);
		}
		return Item.Issue!.Author.Id == loggedInUserId;
	}

	/// <summary>
	///   VoteUp method
	/// </summary>
	/// <param name="comment">The comment to vote up.</param>
	public async Task VoteUp(global::Shared.Models.Comment comment)
	{
		ObjectId loggedInUserId = ObjectId.Empty;
		if (!string.IsNullOrEmpty(LoggedInUser?.Id))
		{
			loggedInUserId = ObjectId.Parse(LoggedInUser.Id);
		}

		if (comment.Author.Id == loggedInUserId)
		{
			return; // Can't vote on your own comments
		}

		if (!comment.UserVotes.Add(LoggedInUser!.Id))
		{
			comment.UserVotes.Remove(LoggedInUser.Id);
		}

		await CommentService.UpVoteComment(comment.Id, LoggedInUser.Id);
	}

	/// <summary>
	///   Gets the text to display for the top part of the vote up button.
	/// </summary>
	/// <param name="comment">The comment to get the vote up information for.</param>
	/// <returns>The text to display for the top part of the vote up button.</returns>
	public string GetUpVoteTopText(global::Shared.Models.Comment comment)
	{
		if (comment.UserVotes.Count > 0)
		{
			return comment.UserVotes.Count.ToString("00");
		}

		ObjectId loggedInUserId = ObjectId.Empty;
		if (!string.IsNullOrEmpty(LoggedInUser?.Id))
		{
			loggedInUserId = ObjectId.Parse(LoggedInUser.Id);
		}

		return comment.Author.Id == loggedInUserId ? "Awaiting" : "Click To";
	}

	/// <summary>
	///   Gets the text to display for the bottom part of the vote up button.
	/// </summary>
	/// <param name="comment">The comment to get the vote up information for.</param>
	/// <returns>The text to display for the bottom part of the vote up button.</returns>
	public string GetUpVoteBottomText(global::Shared.Models.Comment comment)
	{
		return comment.UserVotes.Count > 1 ? "UpVotes" : "UpVote";
	}

	/// <summary>
	///   Gets the css class to apply to the vote up button.
	/// </summary>
	/// <param name="comment">The comment to get the vote up information for.</param>
	/// <returns>The css class to apply to the vote up button.</returns>
	public string GetVoteCssClass(global::Shared.Models.Comment comment)
	{
		if (comment.UserVotes.Count == 0)
		{
			return "comment-no-votes";
		}

		return comment.UserVotes.Contains(LoggedInUser.Id) ? "comment-not-voted" : "comment-voted";
	}

	/// <summary>
	///   Archives the currently selected comment.
	/// </summary>
	/// <returns>A task representing the asynchronous archiving operation.</returns>
	private async Task ArchiveComment()
	{
		_archivingComment!.ArchivedBy = new UserDto(LoggedInUser);
		_archivingComment!.Archived = true;
		await CommentService.UpdateComment(_archivingComment);
		_archivingComment = null;
	}

	/// <summary>
	///   Sets the currently selected comment as the answer to the issue.
	/// </summary>
	/// <param name="comment">The comment to set as the answer.</param>
	/// <returns>A task representing the asynchronous set answer operation.</returns>
	private async Task SetAnswer(global::Shared.Models.Comment comment)
	{
		comment.IsAnswer = true;
		comment.AnswerSelectedBy = new UserDto(LoggedInUser);
		await CommentService.UpdateComment(comment);
	}

	/// <summary>
	///   Gets the css class to apply to the answer status display for the comment.
	/// </summary>
	/// <param name="comment">The comment to get the answer status css class for.</param>
	/// <returns>The css class to apply to the answer status display for the comment.</returns>
	private static string GetAnswerStatusCssClass(global::Shared.Models.Comment comment)
	{
		return comment.IsAnswer ? "comment-answer-status-answered" : "comment-answer-status-unanswered";
	}
}
