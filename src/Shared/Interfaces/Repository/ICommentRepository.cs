// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     ICommentRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Models;
using Shared.Models.DTOs;

namespace Shared.Interfaces.Repository;

/// <summary>
///   Provides repository methods for comment entities.
/// </summary>
public interface ICommentRepository
{
	/// <summary>
	///   Archives the specified comment asynchronously.
	/// </summary>
	/// <param name="comment">The comment to archive.</param>
	/// <returns>A task representing the asynchronous archive operation.</returns>
	Task ArchiveAsync(Comment comment);

	/// <summary>
	///   Creates a new comment asynchronously.
	/// </summary>
	/// <param name="comment">The comment to create.</param>
	/// <returns>A task representing the asynchronous create operation.</returns>
	Task CreateAsync(Comment comment);

	/// <summary>
	///   Gets a comment by its identifier asynchronously.
	/// </summary>
	/// <param name="itemId">The comment identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the comment.</returns>
	Task<Comment> GetAsync(string itemId);

	/// <summary>
	///   Gets all comments asynchronously.
	/// </summary>
	/// <returns>
	///   A task representing the asynchronous operation. The task result contains a collection of comments,
	///   or <see langword="null" /> if no comments are found.
	/// </returns>
	Task<IEnumerable<Comment>?> GetAllAsync();

	/// <summary>
	///   Gets all comments created by the specified user asynchronously.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains a collection of comments.</returns>
	Task<IEnumerable<Comment>> GetByUserAsync(string userId);

	/// <summary>
	///   Gets all comments for the specified issue asynchronously.
	/// </summary>
	/// <param name="issue">The issue.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains a collection of comments.</returns>
	Task<IEnumerable<Comment>> GetByIssueAsync(IssueDto issue);

	/// <summary>
	///   Updates the specified comment asynchronously.
	/// </summary>
	/// <param name="itemId">The comment identifier.</param>
	/// <param name="comment">The comment to update.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	Task UpdateAsync(string itemId, Comment comment);

	/// <summary>
	///   Registers an upvote for the specified comment asynchronously.
	/// </summary>
	/// <param name="itemId">The comment identifier.</param>
	/// <param name="userId">The user identifier.</param>
	/// <returns>A task representing the asynchronous upvote operation.</returns>
	Task UpVoteAsync(string itemId, string userId);
}
