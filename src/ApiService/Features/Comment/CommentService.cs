// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CommentService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Interfaces.Services;

namespace Shared.Features.Comment;

/// <summary>
///   CommentService class
/// </summary>
public class CommentService(ICommentRepository repository, IMemoryCache cache) : ICommentService
{
	private const string CacheName = "CommentData";

	/// <summary>
	///   ArchiveComment method
	/// </summary>
	/// <param name="comment">CommentModel</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task ArchiveComment(CommentModel comment)
	{
		ArgumentNullException.ThrowIfNull(comment);

		cache.Remove(CacheName);

		return repository.ArchiveAsync(comment);
	}

	/// <summary>
	///   CreateComment method
	/// </summary>
	/// <param name="comment">CommentModel</param>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task CreateComment(CommentModel comment)
	{
		ArgumentNullException.ThrowIfNull(comment);

		await repository.CreateAsync(comment);
	}

	/// <summary>
	///   GetComment method
	/// </summary>
	/// <param name="commentId">string</param>
	/// <returns>Task of CommentModel</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<CommentModel> GetComment(string commentId)
	{
		ArgumentException.ThrowIfNullOrEmpty(commentId);

		CommentModel result = await repository.GetAsync(commentId);

		return result;
	}

	/// <summary>
	///   GetComments method
	/// </summary>
	/// <returns>Task of List CommentModels</returns>
	public async Task<List<CommentModel>> GetComments()
	{
		List<CommentModel>? output = cache.Get<List<CommentModel>>(CacheName);

		if (output is not null)
		{
			return output;
		}

		IEnumerable<CommentModel>? results = await repository.GetAllAsync();

		output = results!.Where(x => !x.Archived).ToList();

		cache.Set(CacheName, output, TimeSpan.FromMinutes(1));

		return output;
	}

	/// <summary>
	///   GetCommentsByUser method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of List CommentModels</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<List<CommentModel>> GetCommentsByUser(string userId)
	{
		ArgumentException.ThrowIfNullOrEmpty(userId);

		IEnumerable<CommentModel> results = await repository.GetByUserAsync(userId);

		return results.ToList();
	}

	/// <summary>
	///   GetCommentsByIssue method
	/// </summary>
	/// <param name="issue">BasicIssueModel</param>
	/// <returns>Task of List CommentModels</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<List<CommentModel>> GetCommentsByIssue(BasicIssueModel issue)
	{
		ArgumentNullException.ThrowIfNull(issue);

		IEnumerable<CommentModel> results = await repository.GetByIssueAsync(issue);

		return results.ToList();
	}

	/// <summary>
	///   UpdateComment method
	/// </summary>
	/// <param name="comment">CommentModel</param>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task UpdateComment(CommentModel comment)
	{
		ArgumentNullException.ThrowIfNull(comment);

		await repository.UpdateAsync(comment.Id, comment);

		cache.Remove(CacheName);
	}

	/// <summary>
	///   UpVoteComment method
	/// </summary>
	/// <param name="commentId">string</param>
	/// <param name="userId">string</param>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task UpVoteComment(string commentId, string userId)
	{
		ArgumentException.ThrowIfNullOrEmpty(commentId);

		ArgumentException.ThrowIfNullOrEmpty(userId);

		await repository.UpVoteAsync(commentId, userId);

		cache.Remove(CacheName);
	}
}