// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ArticleRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

using ApiService.DataAccess;

using Shared.Abstractions;

namespace ApiService.Repositories;

/// <summary>
/// Article repository implementation using native MongoDB.Driver with factory pattern.
/// </summary>
public class ArticleRepository
(
				IMongoDbContextFactory contextFactory,
				ILogger<ArticleRepository>? logger = null
) : IArticleRepository
{
	private readonly ILogger<ArticleRepository> _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticleRepository>.Instance;

	/// <summary>
	/// Gets an article by its unique identifier.
	/// </summary>
	/// <param name="id">The ObjectId of the article.</param>
	/// <returns>A <see cref="Result{Article}"/> containing the article if found, or an error message.</returns>
	public async Task<Result<Article?>> GetArticleByIdAsync(ObjectId id)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();

			Article? article = await context.Articles
					.Find(a => a.Id == id)
					.FirstOrDefaultAsync();

			return Result.Ok(article);
		}
		catch (Exception ex)
		{
			return Result.Fail<Article?>($"Error getting article by id: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets an article by its slug.
	/// </summary>
	/// <param name="slug">The slug of the article.</param>
	/// <returns>A <see cref="Result{Article}"/> containing the article if found, or an error message.</returns>
	public async Task<Result<Article?>> GetArticleBySlugAsync(string slug)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();

			Article? article = await context.Articles
					.Find(a => a.Slug == slug)
					.FirstOrDefaultAsync();

			return Result.Ok(article);
		}
		catch (OperationCanceledException ex)
		{
			_logger.LogWarning(ex, "Operation cancelled while getting article by slug: {Slug}", slug);
			return Result.Fail<Article?>("Request was cancelled");
		}
		catch (MongoException ex)
		{
			_logger.LogError(ex, "Database error getting article by slug: {Slug}", slug);
			return Result.Fail<Article?>($"Database error: {ex.Message}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error getting article by slug: {Slug}", slug);
			return Result.Fail<Article?>($"Error getting article: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets all non-archived articles.
	/// </summary>
	/// <returns>A <see cref="Result{IEnumerable{Article}}"/> containing the articles or an error message.</returns>
	public async Task<Result<IEnumerable<Article>?>> GetArticles()
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();

			List<Article>? articles = await context.Articles
					.Find(_ => true)
					.ToListAsync();

			return Result.Ok<IEnumerable<Article>?>(articles);
		}
		catch (Exception ex)
		{
			return Result.Fail<IEnumerable<Article>?>($"Error getting articles: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets articles matching a specified predicate.
	/// </summary>
	/// <param name="where">The predicate to filter articles.</param>
	/// <returns>A <see cref="Result{IEnumerable{Article}}"/> containing the filtered articles or an error message.</returns>
	public async Task<Result<IEnumerable<Article>?>> GetArticles(Expression<Func<Article, bool>> where)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();

			List<Article>? articles = await context.Articles
					.Find(where)
					.ToListAsync();

			return Result.Ok<IEnumerable<Article>?>(articles);
		}
		catch (Exception ex)
		{
			return Result.Fail<IEnumerable<Article>?>($"Error getting articles: {ex.Message}");
		}
	}

	/// <summary>
	/// Adds a new article to the database.
	/// </summary>
	/// <param name="post">The article to add.</param>
	/// <returns>A <see cref="Result{Article}"/> containing the added article or an error message.</returns>
	public async Task<Result<Article>> AddArticle(Article post)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();
			await context.Articles.InsertOneAsync(post);

			return Result.Ok(post);
		}
		catch (Exception ex)
		{
			return Result.Fail<Article>($"Error adding article: {ex.Message}");
		}
	}

	/// <summary>
	/// Updates an existing article in the database.
	/// </summary>
	/// <param name="post">The article to update.</param>
	/// <returns>A <see cref="Result{Article}"/> containing the updated article or an error message.</returns>
	public async Task<Result<Article>> UpdateArticle(Article post)
	{
		try
		{
			IMongoDbContext context = contextFactory.CreateDbContext();

			// Capture the expected version from the caller (optimistic concurrency)
			int expectedVersion = post.Version;

			// Prepare the filter to match Id and either matching version or missing version field (some documents may not include the version field)
			var idFilter = Builders<Article>.Filter.Eq(a => a.Id, post.Id);
			var versionFilter = Builders<Article>.Filter.Or(
				Builders<Article>.Filter.Eq(a => a.Version, expectedVersion),
				Builders<Article>.Filter.Exists("version", false)
			);
			var filter = Builders<Article>.Filter.And(idFilter, versionFilter);

			// Create a replacement clone and increment its version. Do not mutate caller object.
			var replacement = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<Article>(post.ToBsonDocument());
			replacement.Version = expectedVersion + 1;
			replacement.ModifiedOn = DateTimeOffset.UtcNow;

			_logger.LogInformation("ArticleRepository.UpdateArticle: Attempting FindOneAndReplace for Id={Id} ExpectedVersion={ExpectedVersion} ReplacementVersion={ReplacementVersion}", post.Id, expectedVersion, replacement.Version);

			// Use FindOneAndReplaceAsync to atomically find and replace the document
			// This is more atomic than ReplaceOne and better handles concurrent updates
			var options = new FindOneAndReplaceOptions<Article>()
			{
				ReturnDocument = ReturnDocument.After,
				IsUpsert = false
			};

			var result = await context.Articles.FindOneAndReplaceAsync(filter, replacement, options);

			if (result != null)
			{
				_logger.LogInformation("ArticleRepository.UpdateArticle: FindOneAndReplace succeeded. NewVersion={NewVersion}", result.Version);
				return Result.Ok(result);
			}

			// No document matched the id+version filter -> concurrency conflict
			var server = await context.Articles.Find(a => a.Id == post.Id).FirstOrDefaultAsync();
			var serverDto = server is null ? null : server.ToDto(canEdit: false);
			var changed2 = new List<string>();
			if (server is not null)
			{
				if (server.Title != post.Title) changed2.Add("Title");
				if (server.Introduction != post.Introduction) changed2.Add("Introduction");
				if (server.Content != post.Content) changed2.Add("Content");
				if (server.CoverImageUrl != post.CoverImageUrl) changed2.Add("CoverImageUrl");
				if (server.IsPublished != post.IsPublished) changed2.Add("IsPublished");
				if (server.IsArchived != post.IsArchived) changed2.Add("IsArchived");
			}
			var details2 = new Web.Infrastructure.ConcurrencyConflictInfo(server?.Version ?? -1, serverDto, changed2);
			_logger.LogInformation("ArticleRepository.UpdateArticle: FindOneAndReplace did not find a matching document (conflict)");
			return Result.Fail<Article>("Concurrency conflict: article was modified by another process", Shared.Abstractions.ResultErrorCode.Concurrency, details2);
		}
		catch (Exception ex)
		{
			return Result.Fail<Article>($"Error updating article: {ex.Message}");
		}
	}

	/// <summary>
	/// Archives an article by its slug.
	/// </summary>
	/// <param name="slug">The slug of the article to archive.</param>
	public async Task ArchiveArticle(string slug)
	{
		IMongoDbContext context = contextFactory.CreateDbContext();
		UpdateDefinition<Article>? update = Builders<Article>.Update.Set(a => a.IsArchived, true);
		await context.Articles.UpdateOneAsync(a => a.Slug == slug, update);
	}

}

