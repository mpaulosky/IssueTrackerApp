// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     ICategoryRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Models;

namespace Shared.Interfaces.Repository;

public interface ICategoryRepository
{
	Task ArchiveAsync(Category category);

	Task CreateAsync(Category category);

	Task<Category> GetAsync(string? itemId);

	Task<IEnumerable<Category>> GetAllAsync();

	Task UpdateAsync(string? itemId, Category category);
}
