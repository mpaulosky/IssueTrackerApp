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
	Task ArchiveAsync(Shared.Models.Issue issue);

	Task CreateAsync(Shared.Models.Issue issue);

	Task<Shared.Models.Issue> GetAsync(string itemId);

	Task<IEnumerable<Shared.Models.Issue>> GetAllAsync();

	Task<IEnumerable<Shared.Models.Issue>> GetApprovedAsync();

	Task<IEnumerable<Shared.Models.Issue>> GetByUserAsync(string userId);

	Task<IEnumerable<Shared.Models.Issue>> GetWaitingForApprovalAsync();

	Task UpdateAsync(string itemId, Shared.Models.Issue issue);
}
