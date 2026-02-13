// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     ICategoryService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Models;

namespace Shared.Interfaces.Services;

public interface ICategoryService
{
	Task ArchiveCategory(Category category);

	Task CreateCategory(Category category);

	Task<Category> GetCategory(string? categoryId);

	Task<List<Category>> GetCategories();

	Task UpdateCategory(Category category);
}
