// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IIssueService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Services;

public interface IIssueService
{
	Task ArchiveIssue(Shared.Models.Issue issue);

	Task CreateIssue(Shared.Models.Issue issue);

	Task<Shared.Models.Issue> GetIssue(string? issueId);

	Task<List<Shared.Models.Issue>> GetIssues();

	Task<List<Shared.Models.Issue>> GetIssuesByUser(string userId);

	Task<List<Shared.Models.Issue>> GetApprovedIssues();

	Task<List<Shared.Models.Issue>> GetIssuesWaitingForApproval();

	Task UpdateIssue(Shared.Models.Issue issue);
}
