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
public class CommentRepository(IMongoDbContextFactory context) : ICommentRepository
{
	private readonly IMongoCollection<CommentModel> _commentCollection =
		context.GetCollection<CommentModel>(GetCollectionName(nameof(CommentModel)));

	/// <summary>
	///   Archive the comment by setting the Archived property to true
	/// </summary>
	/// <param name="comment"></param>
	/// <returns>Task</returns>
	public async Task ArchiveAsync(CommentModel comment)
	{
		ArgumentNullException.ThrowIfNull(comment, nameof(comment));

		// Archive the category
		comment.Archived = true;

		await UpdateAsync(comment.Id, comment);
	}

	/// <summary>
	///   CreateComment method
	/// </summary>
	/// <param name="comment">CommentModel</param>
	/// <exception cref="Exception"></exception>
	public async Task CreateAsync(CommentModel comment)
	{
		ArgumentNullException.ThrowIfNull(comment, nameof(comment));

		await _commentCollection.InsertOneAsync(comment);
	}

	/// <summary>
	///   GetComment method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of CommentModel</returns>
	public async Task<CommentModel> GetAsync(string itemId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(itemId, nameof(itemId));

		ObjectId objectId = new(itemId);

		FilterDefinition<CommentModel>? filter = Builders<CommentModel>.Filter.Eq("_id", objectId);

		CommentModel? result = (await _commentCollection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   GetComments method
	/// </summary>
	/// <returns>Task of IEnumerable CommentModel</returns>
	public async Task<IEnumerable<CommentModel>?> GetAllAsync()
	{
		FilterDefinition<CommentModel>? filter = Builders<CommentModel>.Filter.Empty;

		List<CommentModel>? results = (await _commentCollection.FindAsync(filter)).ToList();

		return results;
	}

	/// <summary>
	///   GetCommentsByIssue method
	/// </summary>
	/// <param name="issue">BasicIssueModel</param>
	/// <returns>Task of IEnumerable CommentModel</returns>
	public async Task<IEnumerable<CommentModel>> GetByIssueAsync(BasicIssueModel issue)
	{
		ArgumentNullException.ThrowIfNull(issue, nameof(issue));

		List<CommentModel>? results = (await _commentCollection
				.FindAsync(s => s.Issue.Id == issue.Id))
			.ToList();

		return results;
	}

	/// <summary>
	///   GetCommentsByUser method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of IEnumerable CommentModel</returns>
	public async Task<IEnumerable<CommentModel>> GetByUserAsync(string userId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

		List<CommentModel>? results = (await _commentCollection.FindAsync(s => s.Author.Id == userId)).ToList();

		return results;
	}

	/// <summary>
	///   UpdateComment method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="comment">CommentModel</param>
	public async Task UpdateAsync(string itemId, CommentModel comment)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<CommentModel>? filter = Builders<CommentModel>.Filter.Eq("_id", objectId);

		await _commentCollection.ReplaceOneAsync(filter, comment);
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

		FilterDefinition<CommentModel>? filterComment = Builders<CommentModel>.Filter.Eq("_id", objectId);

		CommentModel? comment = (await _commentCollection.FindAsync(filterComment)).FirstOrDefault();

		bool isUpVote = comment.UserVotes.Add(userId);

		if (!isUpVote)
		{
			comment.UserVotes.Remove(userId);
		}

		await _commentCollection.ReplaceOneAsync(s => s.Id == itemId, comment);
	}
}
