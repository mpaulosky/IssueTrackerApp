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
	Task ArchiveAsync(StatusModel status);

	Task CreateAsync(StatusModel status);

	Task<StatusModel> GetAsync(string itemId);

	Task<IEnumerable<StatusModel>> GetAllAsync();

	Task UpdateAsync(string itemId, StatusModel status);
}