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
	/// <param name="comment">Comment</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task ArchiveComment(Shared.Models.Comment comment)
	{
		ArgumentNullException.ThrowIfNull(comment);

		cache.Remove(CacheName);

		return repository.ArchiveAsync(comment);
	}

	/// <summary>
	///   CreateComment method
	/// </summary>
	/// <param name="comment">Comment</param>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task CreateComment(Shared.Models.Comment comment)
	{
		ArgumentNullException.ThrowIfNull(comment);

		await repository.CreateAsync(comment);
	}

	/// <summary>
	///   GetComment method
	/// </summary>
	/// <param name="commentId">string</param>
	/// <returns>Task of Comment</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<Shared.Models.Comment> GetComment(string commentId)
	{
		ArgumentException.ThrowIfNullOrEmpty(commentId);

		Shared.Models.Comment result = await repository.GetAsync(commentId);

		return result;
	}

	/// <summary>
	///   GetComments method
	/// </summary>
	/// <returns>Task of List Comments</returns>
	public async Task<List<Shared.Models.Comment>> GetComments()
	{
		List<Shared.Models.Comment>? output = cache.Get<List<Shared.Models.Comment>>(CacheName);

		if (output is not null)
		{
			return output;
		}

		IEnumerable<Shared.Models.Comment>? results = await repository.GetAllAsync();

		output = results!.Where(x => !x.Archived).ToList();

		cache.Set(CacheName, output, TimeSpan.FromMinutes(1));

		return output;
	}

	/// <summary>
	///   GetCommentsByUser method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of List Comments</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<List<Shared.Models.Comment>> GetCommentsByUser(string userId)
	{
		ArgumentException.ThrowIfNullOrEmpty(userId);

		IEnumerable<Shared.Models.Comment> results = await repository.GetByUserAsync(userId);

		return results.ToList();
	}

	/// <summary>
	///   GetCommentsByIssue method
	/// </summary>
	/// <param name="issue">IssueDto</param>
	/// <returns>Task of List Comments</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<List<Shared.Models.Comment>> GetCommentsByIssue(IssueDto issue)
	{
		ArgumentNullException.ThrowIfNull(issue);

		IEnumerable<Shared.Models.Comment> results = await repository.GetByIssueAsync(issue);

		return results.ToList();
	}

	/// <summary>
	///   UpdateComment method
	/// </summary>
	/// <param name="comment">Comment</param>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task UpdateComment(Shared.Models.Comment comment)
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
