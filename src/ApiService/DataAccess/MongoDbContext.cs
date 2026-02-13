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
///   Provides a MongoDB context implementation using the native MongoDB Driver.
///   This option provides direct access to MongoDB collections without EF Core overhead.
/// </summary>
// ReSharper disable once UnusedType.Global
public class MongoDbContext : IMongoDbContext
{
	private readonly IMongoDatabase _database;

	/// <summary>
	///   Initializes a new instance of the <see cref="MongoDbContext" /> class.
	/// </summary>
	/// <param name="mongoClient">The MongoDB client.</param>
	/// <param name="databaseName">The database name.</param>
	public MongoDbContext(IMongoClient mongoClient, string databaseName)
	{
		_database = mongoClient.GetDatabase(databaseName);
	}

	/// <summary>
	///   Gets the issues collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of issues.
	/// </value>
	public IMongoCollection<Issue> Issues => _database.GetCollection<Issue>("issues");

	/// <summary>
	///   Gets the categories collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of categories.
	/// </value>
	public IMongoCollection<Category> Categories => _database.GetCollection<Category>("categories");

	/// <summary>
	///   Gets the comments collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of comments.
	/// </value>
	public IMongoCollection<Comment> Comments => _database.GetCollection<Comment>("comments");

	/// <summary>
	///   Gets the statuses collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of statuses.
	/// </value>
	public IMongoCollection<Status> Statuses => _database.GetCollection<Status>("statuses");

	/// <summary>
	///   Gets the users collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of users.
	/// </value>
	public IMongoCollection<User> Users => _database.GetCollection<User>("users");

	/// <summary>
	///   Gets the MongoDB database instance.
	/// </summary>
	/// <value>
	///   The MongoDB database instance.
	/// </value>
	public IMongoDatabase Database => _database;

	/// <summary>
	///   Releases the resources used by the <see cref="MongoDbContext" />.
	/// </summary>
	public void Dispose()
	{
		// MongoDB client manages connection pooling, no explicit disposal needed
		GC.SuppressFinalize(this);
	}
}
