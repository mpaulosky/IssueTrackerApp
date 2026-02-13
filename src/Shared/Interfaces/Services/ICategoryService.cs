// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     ICategoryService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Services;

public interface ICategoryService
{
	Task ArchiveCategory(CategoryModel category);

	Task CreateCategory(CategoryModel category);

	Task<CategoryModel> GetCategory(string? categoryId);

	Task<List<CategoryModel>> GetCategories();

	Task UpdateCategory(CategoryModel category);
}
