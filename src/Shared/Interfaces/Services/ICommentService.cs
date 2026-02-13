// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     ICommentService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Models;
using Shared.Models.DTOs;

namespace Shared.Interfaces.Services;

/// <summary>
///   Provides methods for managing comments.
/// </summary>
public interface ICommentService
{
	/// <summary>
	///   Archives the specified comment.
	/// </summary>
	/// <param name="comment">The comment to archive.</param>
	/// <returns>A task representing the asynchronous archive operation.</returns>
	Task ArchiveComment(Comment comment);

	/// <summary>
	///   Creates a new comment.
	/// </summary>
	/// <param name="comment">The comment to create.</param>
	/// <returns>A task representing the asynchronous create operation.</returns>
	Task CreateComment(Comment comment);

	/// <summary>
	///   Gets a comment by its identifier.
	/// </summary>
	/// <param name="commentId">The comment identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the comment.</returns>
	Task<Comment> GetComment(string commentId);

	/// <summary>
	///   Gets all comments.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of comments.</returns>
	Task<List<Comment>> GetComments();

	/// <summary>
	///   Gets all comments created by the specified user.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of comments.</returns>
	Task<List<Comment>> GetCommentsByUser(string userId);

	/// <summary>
	///   Gets all comments for the specified issue.
	/// </summary>
	/// <param name="issue">The issue.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of comments.</returns>
	Task<List<Comment>> GetCommentsByIssue(IssueDto issue);

	/// <summary>
	///   Updates the specified comment.
	/// </summary>
	/// <param name="comment">The comment to update.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	Task UpdateComment(Comment comment);

	/// <summary>
	///   Registers an upvote for the specified comment.
	/// </summary>
	/// <param name="commentId">The comment identifier.</param>
	/// <param name="userId">The user identifier.</param>
	/// <returns>A task representing the asynchronous upvote operation.</returns>
	Task UpVoteComment(string commentId, string userId);
}
