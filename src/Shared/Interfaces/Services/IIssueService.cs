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
	Task ArchiveIssue(IssueModel issue);

	Task CreateIssue(IssueModel issue);

	Task<IssueModel> GetIssue(string? issueId);

	Task<List<IssueModel>> GetIssues();

	Task<List<IssueModel>> GetIssuesByUser(string userId);

	Task<List<IssueModel>> GetApprovedIssues();

	Task<List<IssueModel>> GetIssuesWaitingForApproval();

	Task UpdateIssue(IssueModel issue);
}