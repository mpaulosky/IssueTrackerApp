// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IIssueRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Repository;

/// <summary>
///   Provides repository methods for issue entities.
/// </summary>
public interface IIssueRepository
{
	/// <summary>
	///   Archives the specified issue asynchronously.
	/// </summary>
	/// <param name="issue">The issue to archive.</param>
	/// <returns>A task representing the asynchronous archive operation.</returns>
	Task ArchiveAsync(Shared.Models.Issue issue);

	/// <summary>
	///   Creates a new issue asynchronously.
	/// </summary>
	/// <param name="issue">The issue to create.</param>
	/// <returns>A task representing the asynchronous create operation.</returns>
	Task CreateAsync(Shared.Models.Issue issue);

	/// <summary>
	///   Gets an issue by its identifier asynchronously.
	/// </summary>
	/// <param name="itemId">The issue identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the issue.</returns>
	Task<Shared.Models.Issue> GetAsync(string itemId);

	/// <summary>
	///   Gets all issues asynchronously.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a collection of issues.</returns>
	Task<IEnumerable<Shared.Models.Issue>> GetAllAsync();

	/// <summary>
	///   Gets all approved issues asynchronously.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a collection of approved issues.</returns>
	Task<IEnumerable<Shared.Models.Issue>> GetApprovedAsync();

	/// <summary>
	///   Gets all issues created by the specified user asynchronously.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains a collection of issues.</returns>
	Task<IEnumerable<Shared.Models.Issue>> GetByUserAsync(string userId);

	/// <summary>
	///   Gets all issues waiting for approval asynchronously.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a collection of issues waiting for approval.</returns>
	Task<IEnumerable<Shared.Models.Issue>> GetWaitingForApprovalAsync();

	/// <summary>
	///   Updates the specified issue asynchronously.
	/// </summary>
	/// <param name="itemId">The issue identifier.</param>
	/// <param name="issue">The issue to update.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	Task UpdateAsync(string itemId, Shared.Models.Issue issue);
}
