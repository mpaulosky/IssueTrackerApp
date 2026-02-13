// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbContextFactory.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

namespace ApiService.DataAccess;

/// <summary>
///   A factory for <see cref="MongoDbContext" /> that can create instances
///   with proper configuration.
/// </summary>
public sealed class MongoDbContextFactory : IMongoDbContextFactory
{

	private readonly IMongoDatabase _database;

	public MongoDbContextFactory(IMongoClient mongoClient, string databaseName)
	{
		MongoClient client = (MongoClient)mongoClient;
		_database = client.GetDatabase(databaseName);
	}

	public IMongoDatabase Database => _database;

	public IMongoDbContext CreateDbContext()
	{
		return new MongoDbContext(_database.Client, _database.DatabaseNamespace.DatabaseName);
	}

}
