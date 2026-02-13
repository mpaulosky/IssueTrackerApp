// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IStatusRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Repository;

public interface IStatusRepository
{
	Task ArchiveAsync(Shared.Models.Status status);

	Task CreateAsync(Shared.Models.Status status);

	Task<Shared.Models.Status> GetAsync(string itemId);

	Task<IEnumerable<Shared.Models.Status>> GetAllAsync();

	Task UpdateAsync(string itemId, Shared.Models.Status status);
}
