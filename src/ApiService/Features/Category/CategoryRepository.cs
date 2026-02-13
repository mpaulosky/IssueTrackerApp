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
public class CategoryRepository(IMongoDbContextFactory context) : ICategoryRepository
{
	private readonly IMongoCollection<CategoryModel> _collection =
		context.GetCollection<CategoryModel>(GetCollectionName(nameof(CategoryModel)));

	/// <summary>
	///   Archive Category method
	/// </summary>
	/// <param name="category"></param>
	/// <returns></returns>
	public async Task ArchiveAsync(CategoryModel category)
	{
		// Archive the category
		category.Archived = true;

		await UpdateAsync(category.Id, category);
	}

	/// <summary>
	///   Create Category method
	/// </summary>
	/// <param name="category">CategoryModel</param>
	public async Task CreateAsync(CategoryModel category)
	{
		await _collection.InsertOneAsync(category);
	}

	/// <summary>
	///   Get Category method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of CategoryModel</returns>
	public async Task<CategoryModel> GetAsync(string? itemId)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<CategoryModel>? filter = Builders<CategoryModel>.Filter.Eq("_id", objectId);

		CategoryModel? result = (await _collection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   Get Categories method
	/// </summary>
	/// <returns>Task of IEnumerable CategoryModel</returns>
	public async Task<IEnumerable<CategoryModel>> GetAllAsync()
	{
		FilterDefinition<CategoryModel>? filter = Builders<CategoryModel>.Filter.Empty;

		List<CategoryModel>? result = (await _collection.FindAsync(filter)).ToList();

		return result;
	}

	/// <summary>
	///   Update Category method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="category">CategoryModel</param>
	public async Task UpdateAsync(string? itemId, CategoryModel category)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<CategoryModel>? filter = Builders<CategoryModel>.Filter.Eq("_id", objectId);

		await _collection.ReplaceOneAsync(filter, category);
	}
}