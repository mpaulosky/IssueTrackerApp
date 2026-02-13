// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CategoryService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Interfaces.Services;

namespace Shared.Features.Category;

/// <summary>
///   CategoryService class
/// </summary>
public class CategoryService(ICategoryRepository repository, IMemoryCache cache) : ICategoryService
{
	private const string CacheName = "CategoryData";

	/// <summary>
	///   CreateCategory method
	/// </summary>
	/// <param name="category">Category</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task CreateCategory(Shared.Models.Category category)
	{
		ArgumentNullException.ThrowIfNull(category);

		cache.Remove(CacheName);

		return repository.CreateAsync(category);
	}

	/// <summary>
	///   ArchiveCategory method
	/// </summary>
	/// <param name="category">Category</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task ArchiveCategory(Shared.Models.Category category)
	{
		ArgumentNullException.ThrowIfNull(category);

		cache.Remove(CacheName);

		return repository.ArchiveAsync(category);
	}

	/// <summary>
	///   GetCategory method
	/// </summary>
	/// <param name="categoryId">string</param>
	/// <returns>Task of Category</returns>
	/// <exception cref="ArgumentException">ThrowIfNullOrEmpty(categoryId)</exception>
	public async Task<Shared.Models.Category> GetCategory(string? categoryId)
	{
		ArgumentException.ThrowIfNullOrEmpty(categoryId);

		Shared.Models.Category result = await repository.GetAsync(categoryId);

		return result;
	}

	/// <summary>
	///   GetCategories method
	/// </summary>
	/// <returns>Task of List Category</returns>
	public async Task<List<Shared.Models.Category>> GetCategories()
	{
		List<Shared.Models.Category>? output = cache.Get<List<Shared.Models.Category>>(CacheName);

		if (output is not null)
		{
			return output;
		}

		IEnumerable<Shared.Models.Category> results = await repository.GetAllAsync();

		output = results.ToList();

		cache.Set(CacheName, output, TimeSpan.FromDays(1));

		return output;
	}

	/// <summary>
	///   UpdateCategory method
	/// </summary>
	/// <param name="category">Category</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task UpdateCategory(Shared.Models.Category category)
	{
		ArgumentNullException.ThrowIfNull(category);

		cache.Remove(CacheName);

		return repository.UpdateAsync(category.Id.ToString(), category);
	}
}
