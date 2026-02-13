// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbContext.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

namespace ApiService.DataAccess;

/// <summary>
///   MongoDB Context using native MongoDB Driver
///   This option provides direct access to MongoDB collections without EF Core overhead
/// </summary>

// ReSharper disable once UnusedType.Global
public class MongoDbContext : IMongoDbContext
{

	private readonly IMongoDatabase _database;

	public MongoDbContext(IMongoClient mongoClient, string databaseName)
	{
		_database = mongoClient.GetDatabase(databaseName);
	}

	public IMongoCollection<Issue> Issues => _database.GetCollection<Issue>("issues");

	public IMongoCollection<Category> Categories => _database.GetCollection<Category>("categories");

	public IMongoCollection<Comment> Comments => _database.GetCollection<Comment>("comments");

	public IMongoCollection<Status> Statuses => _database.GetCollection<Status>("statuses");

	public IMongoCollection<User> Users => _database.GetCollection<User>("users");

	public IMongoDatabase Database => _database;

	public void Dispose()
	{
		// MongoDB client manages connection pooling, no explicit disposal needed
		GC.SuppressFinalize(this);
	}

}
