// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

using ApiService.DataAccess;

using Shared.Abstractions;

namespace ApiService.Repositories;

/// <summary>
/// Category repository implementation using native MongoDB.Driver with factory pattern.
/// </summary>
public class CategoryRepository
(
		IMongoDbContextFactory contextFactory
) : ICategoryRepository
{

	/// <summary>
	/// Gets a category by its unique identifier.
	/// </summary>
	/// <param name="id">The ObjectId of the category.</param>
	/// <returns>A <see cref="Result{Category}"/> containing the category if found, or an error message.</returns>
	public async Task<Result<Category>> GetCategoryByIdAsync(ObjectId id)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();
			Category? category = await context.Categories.Find(c => c.Id == id).FirstOrDefaultAsync();

			if (category is null)
				return Result.Fail<Category>("Category not found");

			return Result.Ok(category);
		}
		catch (OperationCanceledException)
		{
			return Result.Fail<Category>("Request was cancelled");
		}
		catch (MongoException ex)
		{
			return Result.Fail<Category>($"Database error: {ex.Message}");
		}
		catch (Exception ex)
		{
			return Result.Fail<Category>($"Error getting category by id: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets a category by its slug.
	/// </summary>
	/// <param name="slug">The slug of the category.</param>
	/// <returns>A <see cref="Result{Category}"/> containing the category if found, or an error message.</returns>
	public async Task<Result<Category>> GetCategory(string slug)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();
			Category? category = await context.Categories.Find(c => c.Slug == slug).FirstOrDefaultAsync();

			if (category is null)
				return Result.Fail<Category>("Category not found");

			return Result.Ok(category);
		}
		catch (Exception ex)
		{
			return Result.Fail<Category>($"Error getting category: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets all non-archived categories.
	/// </summary>
	/// <returns>A <see cref="Result{T}"/> containing a collection of Category or an error message.</returns>
	public async Task<Result<IEnumerable<Category>>> GetCategories()
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();
			List<Category>? categories = await context.Categories.Find(_ => true).ToListAsync();

			if (categories is null)
				return Result.Fail<IEnumerable<Category>>("No categories found");

			return Result.Ok<IEnumerable<Category>>(categories);
		}
		catch (Exception ex)
		{
			return Result.Fail<IEnumerable<Category>>($"Error getting categories: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets categories matching a specified predicate.
	/// </summary>
	/// <param name="where">The predicate to filter categories.</param>
	/// <returns>A <see cref="Result{T}"/> containing a filtered collection of Category or an error message.</returns>
	public async Task<Result<IEnumerable<Category>>> GetCategories(Expression<Func<Category, bool>> where)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();
			List<Category>? categories = await context.Categories.Find(where).ToListAsync();

			if (categories is null)
				return Result.Fail<IEnumerable<Category>>("No categories found");

			return Result.Ok<IEnumerable<Category>>(categories);
		}
		catch (Exception ex)
		{
			return Result.Fail<IEnumerable<Category>>($"Error getting categories: {ex.Message}");
		}
	}

	/// <summary>
	/// Adds a new category to the database.
	/// </summary>
	/// <param name="category">The category to add.</param>
	/// <returns>A <see cref="Result{Category}"/> containing the added category or an error message.</returns>
	public async Task<Result<Category>> AddCategory(Category category)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();
			await context.Categories.InsertOneAsync(category);

			return Result.Ok(category);
		}
		catch (Exception ex)
		{
			return Result.Fail<Category>($"Error adding category: {ex.Message}");
		}
	}

	/// <summary>
	/// Updates an existing category in the database.
	/// </summary>
	/// <param name="category">The category to update.</param>
	/// <returns>A <see cref="Result{Category}"/> containing the updated category or an error message.</returns>
	public async Task<Result<Category>> UpdateCategory(Category category)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();

			// Optimistic concurrency: filter by id and expected version
			int expectedVersion = category.Version;
			var idFilter = Builders<Category>.Filter.Eq(c => c.Id, category.Id);
			var versionFilter = Builders<Category>.Filter.Or(
				Builders<Category>.Filter.Eq(c => c.Version, expectedVersion),
				Builders<Category>.Filter.Exists("version", false)
			);
			var filter = Builders<Category>.Filter.And(idFilter, versionFilter);

			// Clone incoming entity so we can set ModifiedOn and increment version without mutating caller
			var replacement = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<Category>(category.ToBsonDocument());
			replacement.ModifiedOn = DateTimeOffset.UtcNow;
			replacement.Version = expectedVersion + 1;

			var replaceResult = await context.Categories.ReplaceOneAsync(filter, replacement);

			if (replaceResult.IsAcknowledged && replaceResult.ModifiedCount > 0)
			{
				var current = await context.Categories.Find(c => c.Id == category.Id).FirstOrDefaultAsync();
				return current is not null ? Result.Ok(current) : Result.Fail<Category>("Error updating category: replaced but could not read back document");
			}

			// Check if the document exists at all
			var server = await context.Categories.Find(c => c.Id == category.Id).FirstOrDefaultAsync();
			if (server is null)
			{
				// Document doesn't exist - not a concurrency conflict, just not found
				return Result.Ok(category);
			}

			// Concurrency conflict: return structured details for callers
			var changed = new List<string>();
			if (server.CategoryName != category.CategoryName) changed.Add("CategoryName");
			if (server.IsArchived != category.IsArchived) changed.Add("IsArchived");
			var details = new Web.Infrastructure.ConcurrencyConflictInfo(server.Version, null, changed);
			return Result.Fail<Category>("Concurrency conflict: category was modified by another process", Shared.Abstractions.ResultErrorCode.Concurrency, details);
		}
		catch (Exception ex)
		{
			return Result.Fail<Category>($"Error updating category: {ex.Message}");
		}
	}

	/// <summary>
	/// Archives a category by its slug.
	/// </summary>
	/// <param name="slug">The slug of the category to archive.</param>
	public async Task ArchiveCategory(string slug)
	{
		IMongoDbContext context = contextFactory.CreateDbContext();
		UpdateDefinition<Category>? update = Builders<Category>.Update.Set(c => c.IsArchived, true).Set(c => c.ModifiedOn, DateTimeOffset.UtcNow);
		await context.Categories.UpdateOneAsync(c => c.Slug == slug, update);
	}

}
