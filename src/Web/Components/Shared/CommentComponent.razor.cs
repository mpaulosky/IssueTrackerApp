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
	private CommentModel? _archivingComment;
	[Parameter] public CommentModel Item { get; set; } = new();
	[Parameter] public UserModel LoggedInUser { get; set; } = new();

	/// <summary>
	///   Check if the logged in user is able to mark the comment as an answer.
	/// </summary>
	/// <returns>True if the user can mark the comment as an answer, otherwise false.</returns>
	private bool CanMarkAnswer()
	{
		return Item.Issue!.Author.Id == LoggedInUser.Id;
	}

	/// <summary>
	///   VoteUp method
	/// </summary>
	/// <param name="comment">The comment to vote up.</param>
	public async Task VoteUp(CommentModel comment)
	{
		if (comment.Author.Id == LoggedInUser.Id)
		{
			return; // Can't vote on your own comments
		}

		if (!comment.UserVotes.Add(LoggedInUser.Id))
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
	public string GetUpVoteTopText(CommentModel comment)
	{
		if (comment.UserVotes.Count > 0)
		{
			return comment.UserVotes.Count.ToString("00");
		}

		return comment.Author.Id == LoggedInUser.Id ? "Awaiting" : "Click To";
	}

	/// <summary>
	///   Gets the text to display for the bottom part of the vote up button.
	/// </summary>
	/// <param name="comment">The comment to get the vote up information for.</param>
	/// <returns>The text to display for the bottom part of the vote up button.</returns>
	public string GetUpVoteBottomText(CommentModel comment)
	{
		return comment.UserVotes.Count > 1 ? "UpVotes" : "UpVote";
	}

	/// <summary>
	///   Gets the css class to apply to the vote up button.
	/// </summary>
	/// <param name="comment">The comment to get the vote up information for.</param>
	/// <returns>The css class to apply to the vote up button.</returns>
	public string GetVoteCssClass(CommentModel comment)
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
		_archivingComment!.ArchivedBy = new BasicUserModel(LoggedInUser);
		_archivingComment!.Archived = true;
		await CommentService.UpdateComment(_archivingComment);
		_archivingComment = null;
	}

	/// <summary>
	///   Sets the currently selected comment as the answer to the issue.
	/// </summary>
	/// <param name="comment">The comment to set as the answer.</param>
	/// <returns>A task representing the asynchronous set answer operation.</returns>
	private async Task SetAnswer(CommentModel comment)
	{
		comment.IsAnswer = true;
		comment.AnswerSelectedBy = new BasicUserModel(LoggedInUser);
		await CommentService.UpdateComment(comment);
	}

	/// <summary>
	///   Gets the css class to apply to the answer status display for the comment.
	/// </summary>
	/// <param name="comment">The comment to get the answer status css class for.</param>
	/// <returns>The css class to apply to the answer status display for the comment.</returns>
	private static string GetAnswerStatusCssClass(CommentModel comment)
	{
		return comment.IsAnswer ? "comment-answer-status-answered" : "comment-answer-status-unanswered";
	}
}