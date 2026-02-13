// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IssueService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Interfaces.Services;

namespace Shared.Features.Issue;

/// <summary>
///   IssueService class
/// </summary>
public class IssueService(IIssueRepository repository, IMemoryCache cache) : IIssueService
{
	private const string CacheName = "IssueData";

	/// <summary>
	///   ArchiveIssue method
	/// </summary>
	/// <param name="issue">Issue</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task ArchiveIssue(Shared.Models.Issue issue)
	{
		ArgumentNullException.ThrowIfNull(issue);

		cache.Remove(CacheName);

		return repository.ArchiveAsync(issue);
	}

	/// <summary>
	///   CreateIssue method
	/// </summary>
	/// <param name="issue">Issue</param>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task CreateIssue(Shared.Models.Issue issue)
	{
		ArgumentNullException.ThrowIfNull(issue);

		await repository.CreateAsync(issue);
	}

	/// <summary>
	///   GetIssue method
	/// </summary>
	/// <param name="issueId">string</param>
	/// <returns>Task of Issue</returns>
	/// <exception cref="ArgumentException"></exception>
	public async Task<Shared.Models.Issue> GetIssue(string? issueId)
	{
		ArgumentException.ThrowIfNullOrEmpty(issueId);

		Shared.Models.Issue results = await repository.GetAsync(issueId);

		return results;
	}

	/// <summary>
	///   GetIssues method
	/// </summary>
	/// <returns>Task of List Issues</returns>
	public async Task<List<Shared.Models.Issue>> GetIssues()
	{
		List<Shared.Models.Issue>? output = cache.Get<List<Shared.Models.Issue>>(CacheName);

		if (output is not null)
		{
			return output;
		}

		IEnumerable<Shared.Models.Issue> results = await repository.GetAllAsync();

		output = results.ToList();

		cache.Set(CacheName, output, TimeSpan.FromMinutes(1));

		return output;
	}

	/// <summary>
	///   GetIssuesByUser method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of List Issues</returns>
	/// <exception cref="ArgumentException"></exception>
	public async Task<List<Shared.Models.Issue>> GetIssuesByUser(string userId)
	{
		ArgumentException.ThrowIfNullOrEmpty(userId);

		List<Shared.Models.Issue>? output = cache.Get<List<Shared.Models.Issue>>(userId);

		if (output is not null)
		{
			return output;
		}

		IEnumerable<Shared.Models.Issue> results = await repository.GetByUserAsync(userId);

		output = results.ToList();

		cache.Set(userId, output, TimeSpan.FromMinutes(1));

		return output;
	}

	/// <summary>
	///   GetIssuesWaitingForApproval method
	/// </summary>
	/// <returns>Task of List Issues</returns>
	public async Task<List<Shared.Models.Issue>> GetIssuesWaitingForApproval()
	{
		IEnumerable<Shared.Models.Issue> results = await repository.GetWaitingForApprovalAsync();

		return results.ToList();
	}

	/// <summary>
	///   GetApprovedIssues method
	/// </summary>
	/// <returns>Task of List Issues</returns>
	public async Task<List<Shared.Models.Issue>> GetApprovedIssues()
	{
		IEnumerable<Shared.Models.Issue> results = await repository.GetApprovedAsync();

		return results.ToList();
	}

	/// <summary>
	///   UpdateIssue
	/// </summary>
	/// <param name="issue">Issue</param>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task UpdateIssue(Shared.Models.Issue issue)
	{
		ArgumentNullException.ThrowIfNull(issue);

		await repository.UpdateAsync(issue.Id, issue);

		cache.Remove(CacheName);
	}
}
