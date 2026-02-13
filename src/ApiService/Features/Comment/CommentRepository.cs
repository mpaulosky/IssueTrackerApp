// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CommentRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.PlugIns
// =============================================

namespace ApiService.Features.Comment;

/// <summary>
///   CommentRepository class
/// </summary>
public class CommentRepository(IMongoDbContextFactory contextFactory) : ICommentRepository
{
	private readonly IMongoCollection<Shared.Models.Comment> _collection = contextFactory.CreateDbContext().Comments;

	/// <summary>
	///   Archive the comment by setting the Archived property to true
	/// </summary>
	/// <param name="comment"></param>
	/// <returns>Task</returns>
	public async Task ArchiveAsync(Shared.Models.Comment comment)
	{
		ArgumentNullException.ThrowIfNull(comment, nameof(comment));

		// Archive the category
		comment.Archived = true;

		await UpdateAsync(comment.Id, comment);
	}

	/// <summary>
	///   CreateComment method
	/// </summary>
	/// <param name="comment">Comment</param>
	/// <exception cref="Exception"></exception>
	public async Task CreateAsync(Shared.Models.Comment comment)
	{
		ArgumentNullException.ThrowIfNull(comment, nameof(comment));

		await _collection.InsertOneAsync(comment);
	}

	/// <summary>
	///   GetComment method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of Comment</returns>
	public async Task<Shared.Models.Comment> GetAsync(string itemId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(itemId, nameof(itemId));

		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Comment>? filter = Builders<Shared.Models.Comment>.Filter.Eq("_id", objectId);

		Shared.Models.Comment? result = (await _collection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   GetComments method
	/// </summary>
	/// <returns>Task of IEnumerable Comment</returns>
	public async Task<IEnumerable<Shared.Models.Comment>?> GetAllAsync()
	{
		FilterDefinition<Shared.Models.Comment>? filter = Builders<Shared.Models.Comment>.Filter.Empty;

		List<Shared.Models.Comment>? results = (await _collection.FindAsync(filter)).ToList();

		return results;
	}

	/// <summary>
	///   GetCommentsByIssue method
	/// </summary>
	/// <param name="issue">IssueDto</param>
	/// <returns>Task of IEnumerable Comment</returns>
	public async Task<IEnumerable<Shared.Models.Comment>> GetByIssueAsync(IssueDto issue)
	{
		ArgumentNullException.ThrowIfNull(issue, nameof(issue));

		List<Shared.Models.Comment>? results = (await _collection
				.FindAsync(s => s.Issue.Id == issue.Id))
			.ToList();

		return results;
	}

	/// <summary>
	///   GetCommentsByUser method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of IEnumerable Comment</returns>
	public async Task<IEnumerable<Shared.Models.Comment>> GetByUserAsync(string userId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

		ObjectId userObjectId = ObjectId.Parse(userId);
		List<Shared.Models.Comment>? results = (await _collection.FindAsync(s => s.Author.Id == userObjectId)).ToList();

		return results;
	}

	/// <summary>
	///   UpdateComment method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="comment">Comment</param>
	public async Task UpdateAsync(string itemId, Shared.Models.Comment comment)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Comment>? filter = Builders<Shared.Models.Comment>.Filter.Eq("_id", objectId);

		await _collection.ReplaceOneAsync(filter, comment);
	}

	/// <summary>
	///   Up vote Comment method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="userId">string</param>
	/// <exception cref="Exception"></exception>
	public async Task UpVoteAsync(string itemId, string userId)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Comment>? filterComment = Builders<Shared.Models.Comment>.Filter.Eq("_id", objectId);

		Shared.Models.Comment? comment = (await _collection.FindAsync(filterComment)).FirstOrDefault();

		bool isUpVote = comment.UserVotes.Add(userId);

		if (!isUpVote)
		{
			comment.UserVotes.Remove(userId);
		}

		await _collection.ReplaceOneAsync(s => s.Id == itemId, comment);
	}
}
