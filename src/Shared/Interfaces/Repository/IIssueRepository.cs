// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IIssueRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Repository;

public interface IIssueRepository
{
	Task ArchiveAsync(IssueModel issue);

	Task CreateAsync(IssueModel issue);

	Task<IssueModel> GetAsync(string itemId);

	Task<IEnumerable<IssueModel>> GetAllAsync();

	Task<IEnumerable<IssueModel>> GetApprovedAsync();

	Task<IEnumerable<IssueModel>> GetByUserAsync(string userId);

	Task<IEnumerable<IssueModel>> GetWaitingForApprovalAsync();

	Task UpdateAsync(string itemId, IssueModel issue);
}
