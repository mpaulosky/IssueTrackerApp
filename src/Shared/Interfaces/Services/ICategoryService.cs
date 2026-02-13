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

/// <summary>
///   Provides methods for managing categories.
/// </summary>
public interface ICategoryService
{
	/// <summary>
	///   Archives the specified category.
	/// </summary>
	/// <param name="category">The category to archive.</param>
	/// <returns>A task representing the asynchronous archive operation.</returns>
	Task ArchiveCategory(Category category);

	/// <summary>
	///   Creates a new category.
	/// </summary>
	/// <param name="category">The category to create.</param>
	/// <returns>A task representing the asynchronous create operation.</returns>
	Task CreateCategory(Category category);

	/// <summary>
	///   Gets a category by its identifier.
	/// </summary>
	/// <param name="categoryId">The category identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the category.</returns>
	Task<Category> GetCategory(string? categoryId);

	/// <summary>
	///   Gets all categories.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of categories.</returns>
	Task<List<Category>> GetCategories();

	/// <summary>
	///   Updates the specified category.
	/// </summary>
	/// <param name="category">The category to update.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	Task UpdateCategory(Category category);
}
