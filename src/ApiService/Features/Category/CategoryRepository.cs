// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CategoryRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.PlugIns
// =============================================

namespace ApiService.Features.Category;

/// <summary>
///   CategoryRepository class
/// </summary>
public class CategoryRepository(IMongoDbContextFactory contextFactory) : ICategoryRepository
{
	private readonly IMongoCollection<Shared.Models.Category> _collection = contextFactory.CreateDbContext().Categories;

	/// <summary>
	///   Archive Category method
	/// </summary>
	/// <param name="category"></param>
	/// <returns></returns>
	public async Task ArchiveAsync(Shared.Models.Category category)
	{
		// Archive the category
		category.Archived = true;

		await UpdateAsync(category.Id.ToString(), category);
	}

	/// <summary>
	///   Create Category method
	/// </summary>
	/// <param name="category">Category</param>
	public async Task CreateAsync(Shared.Models.Category category)
	{
		await _collection.InsertOneAsync(category);
	}

	/// <summary>
	///   Get Category method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of Category</returns>
	public async Task<Shared.Models.Category> GetAsync(string? itemId)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Category>? filter = Builders<Shared.Models.Category>.Filter.Eq("_id", objectId);

		Shared.Models.Category? result = (await _collection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   Get Categories method
	/// </summary>
	/// <returns>Task of IEnumerable Category</returns>
	public async Task<IEnumerable<Shared.Models.Category>> GetAllAsync()
	{
		FilterDefinition<Shared.Models.Category>? filter = Builders<Shared.Models.Category>.Filter.Empty;

		List<Shared.Models.Category>? result = (await _collection.FindAsync(filter)).ToList();

		return result;
	}

	/// <summary>
	///   Update Category method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="category">Category</param>
	public async Task UpdateAsync(string? itemId, Shared.Models.Category category)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Category>? filter = Builders<Shared.Models.Category>.Filter.Eq("_id", objectId);

		await _collection.ReplaceOneAsync(filter, category);
	}
}
