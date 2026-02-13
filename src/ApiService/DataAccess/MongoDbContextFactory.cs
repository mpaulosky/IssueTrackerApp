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
///   Provides a factory for creating <see cref="MongoDbContext" /> instances with proper configuration.
/// </summary>
public sealed class MongoDbContextFactory : IMongoDbContextFactory
{
	private readonly IMongoDatabase _database;

	/// <summary>
	///   Initializes a new instance of the <see cref="MongoDbContextFactory" /> class.
	/// </summary>
	/// <param name="mongoClient">The MongoDB client.</param>
	/// <param name="databaseName">The database name.</param>
	public MongoDbContextFactory(IMongoClient mongoClient, string databaseName)
	{
		MongoClient client = (MongoClient)mongoClient;
		_database = client.GetDatabase(databaseName);
	}

	/// <summary>
	///   Gets the MongoDB database instance.
	/// </summary>
	/// <value>
	///   The MongoDB database instance.
	/// </value>
	public IMongoDatabase Database => _database;

	/// <summary>
	///   Creates a new instance of <see cref="IMongoDbContext" />.
	/// </summary>
	/// <returns>A new <see cref="IMongoDbContext" /> instance.</returns>
	public IMongoDbContext CreateDbContext()
	{
		return new MongoDbContext(_database.Client, _database.DatabaseNamespace.DatabaseName);
	}
}
