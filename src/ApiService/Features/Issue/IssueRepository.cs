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
public class IssueRepository(IMongoDbContextFactory context) : IIssueRepository
{
	private readonly IMongoCollection<IssueModel> _issueCollection =
		context.GetCollection<IssueModel>(GetCollectionName(nameof(IssueModel)));

	/// <summary>
	///   Archive Issue method
	/// </summary>
	/// <param name="issue">IssueModel</param>
	/// <returns>Task</returns>
	public async Task ArchiveAsync(IssueModel issue)
	{
		// Archive the category
		issue.Archived = true;

		await UpdateAsync(issue.Id, issue);
	}

	/// <summary>
	///   CreateIssue method
	/// </summary>
	/// <param name="issue">IssueModel</param>
	/// <exception cref="Exception"></exception>
	public async Task CreateAsync(IssueModel issue)
	{
		await _issueCollection.InsertOneAsync(issue);
	}

	/// <summary>
	///   GetIssue method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of IssueModel</returns>
	public async Task<IssueModel> GetAsync(string itemId)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<IssueModel>? filter = Builders<IssueModel>.Filter.Eq("_id", objectId);

		IssueModel? result = (await _issueCollection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   GetIssues method
	/// </summary>
	/// <returns>Task of IEnumerable IssueModel</returns>
	public async Task<IEnumerable<IssueModel>> GetAllAsync()
	{
		FilterDefinition<IssueModel>? filter = Builders<IssueModel>.Filter.Empty;

		List<IssueModel>? results = (await _issueCollection.FindAsync(filter)).ToList();

		return results;
	}

	/// <summary>
	///   GetIssuesWaitingForApproval method
	/// </summary>
	/// <returns>Task of IEnumerable IssueModel</returns>
	public async Task<IEnumerable<IssueModel>> GetWaitingForApprovalAsync()
	{
		IEnumerable<IssueModel> output = await GetAllAsync();

		List<IssueModel> results = output.Where(x => !(x is { ApprovedForRelease: true }) && !x.Rejected).ToList();

		return results;
	}

	/// <summary>
	///   GetApprovedIssues method
	/// </summary>
	/// <returns>Task of IEnumerable IssueModel</returns>
	public async Task<IEnumerable<IssueModel>> GetApprovedAsync()
	{
		IEnumerable<IssueModel> output = await GetAllAsync();

		List<IssueModel> results = output.Where(x => x is { ApprovedForRelease: true, Rejected: false }).ToList();

		return results;
	}

	/// <summary>
	///   GetUserIssues method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of IEnumerable IssueModel</returns>
	public async Task<IEnumerable<IssueModel>> GetByUserAsync(string userId)
	{
		List<IssueModel>? results = (await _issueCollection.FindAsync(s => s.Author.Id == userId)).ToList();

		return results;
	}

	/// <summary>
	///   UpdateIssue method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="issue">IssueModel</param>
	public async Task UpdateAsync(string itemId, IssueModel issue)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<IssueModel>? filter = Builders<IssueModel>.Filter.Eq("_id", objectId);

		await _issueCollection.ReplaceOneAsync(filter, issue);
	}
}