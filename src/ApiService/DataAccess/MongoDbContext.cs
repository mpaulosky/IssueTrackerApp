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

	public IMongoCollection<Article> Articles => _database.GetCollection<Article>("Articles");

	public IMongoCollection<Category> Categories => _database.GetCollection<Category>("Categories");

	public IMongoDatabase Database => _database;

}
