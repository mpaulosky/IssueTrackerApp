// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IssueRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.PlugIns
// =============================================

namespace ApiService.Features.Issue;

/// <summary>
///   IssueRepository class
/// </summary>
public class IssueRepository(IMongoDbContextFactory contextFactory) : IIssueRepository
{
	private readonly IMongoCollection<Shared.Models.Issue> _collection = contextFactory.CreateDbContext().Issues;

	/// <summary>
	///   Archive Issue method
	/// </summary>
	/// <param name="issue">Issue</param>
	/// <returns>Task</returns>
	public async Task ArchiveAsync(Shared.Models.Issue issue)
	{
		// Archive the category
		issue.Archived = true;

		await UpdateAsync(issue.Id, issue);
	}

	/// <summary>
	///   CreateIssue method
	/// </summary>
	/// <param name="issue">Issue</param>
	/// <exception cref="Exception"></exception>
	public async Task CreateAsync(Shared.Models.Issue issue)
	{
		await _collection.InsertOneAsync(issue);
	}

	/// <summary>
	///   GetIssue method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of Issue</returns>
	public async Task<Shared.Models.Issue> GetAsync(string itemId)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Issue>? filter = Builders<Shared.Models.Issue>.Filter.Eq("_id", objectId);

		Shared.Models.Issue? result = (await _collection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   GetIssues method
	/// </summary>
	/// <returns>Task of IEnumerable Issue</returns>
	public async Task<IEnumerable<Shared.Models.Issue>> GetAllAsync()
	{
		FilterDefinition<Shared.Models.Issue>? filter = Builders<Shared.Models.Issue>.Filter.Empty;

		List<Shared.Models.Issue>? results = (await _collection.FindAsync(filter)).ToList();

		return results;
	}

	/// <summary>
	///   GetIssuesWaitingForApproval method
	/// </summary>
	/// <returns>Task of IEnumerable Issue</returns>
	public async Task<IEnumerable<Shared.Models.Issue>> GetWaitingForApprovalAsync()
	{
		IEnumerable<Shared.Models.Issue> output = await GetAllAsync();

		List<Shared.Models.Issue> results = output.Where(x => !(x is { ApprovedForRelease: true }) && !x.Rejected).ToList();

		return results;
	}

	/// <summary>
	///   GetApprovedIssues method
	/// </summary>
	/// <returns>Task of IEnumerable Issue</returns>
	public async Task<IEnumerable<Shared.Models.Issue>> GetApprovedAsync()
	{
		IEnumerable<Shared.Models.Issue> output = await GetAllAsync();

		List<Shared.Models.Issue> results = output.Where(x => x is { ApprovedForRelease: true, Rejected: false }).ToList();

		return results;
	}

	/// <summary>
	///   GetUserIssues method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of IEnumerable Issue</returns>
	public async Task<IEnumerable<Shared.Models.Issue>> GetByUserAsync(string userId)
	{
		ObjectId userObjectId = new(userId);
		List<Shared.Models.Issue>? results = (await _collection.FindAsync(s => s.Author.Id == userObjectId)).ToList();

		return results;
	}

	/// <summary>
	///   UpdateIssue method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="issue">Issue</param>
	public async Task UpdateAsync(string itemId, Shared.Models.Issue issue)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Issue>? filter = Builders<Shared.Models.Issue>.Filter.Eq("_id", objectId);

		await _collection.ReplaceOneAsync(filter, issue);
	}
}
