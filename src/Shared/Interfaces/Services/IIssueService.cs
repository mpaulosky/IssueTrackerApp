// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IIssueService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Services;

/// <summary>
///   Provides methods for managing issues.
/// </summary>
public interface IIssueService
{
	/// <summary>
	///   Archives the specified issue.
	/// </summary>
	/// <param name="issue">The issue to archive.</param>
	/// <returns>A task representing the asynchronous archive operation.</returns>
	Task ArchiveIssue(Shared.Models.Issue issue);

	/// <summary>
	///   Creates a new issue.
	/// </summary>
	/// <param name="issue">The issue to create.</param>
	/// <returns>A task representing the asynchronous create operation.</returns>
	Task CreateIssue(Shared.Models.Issue issue);

	/// <summary>
	///   Gets an issue by its identifier.
	/// </summary>
	/// <param name="issueId">The issue identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the issue.</returns>
	Task<Shared.Models.Issue> GetIssue(string? issueId);

	/// <summary>
	///   Gets all issues.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of issues.</returns>
	Task<List<Shared.Models.Issue>> GetIssues();

	/// <summary>
	///   Gets all issues created by the specified user.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of issues.</returns>
	Task<List<Shared.Models.Issue>> GetIssuesByUser(string userId);

	/// <summary>
	///   Gets all approved issues.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of approved issues.</returns>
	Task<List<Shared.Models.Issue>> GetApprovedIssues();

	/// <summary>
	///   Gets all issues waiting for approval.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of issues waiting for approval.</returns>
	Task<List<Shared.Models.Issue>> GetIssuesWaitingForApproval();

	/// <summary>
	///   Updates the specified issue.
	/// </summary>
	/// <param name="issue">The issue to update.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	Task UpdateIssue(Shared.Models.Issue issue);
}
