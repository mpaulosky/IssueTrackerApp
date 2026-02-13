// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IMongoDbContext.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

using Shared.Models;

namespace ApiService.DataAccess;

/// <summary>
///   Provides an interface for MongoDB context using the native driver.
/// </summary>
public interface IMongoDbContext : IDisposable
{
	/// <summary>
	///   Gets the issues collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of issues.
	/// </value>
	IMongoCollection<Issue> Issues { get; }

	/// <summary>
	///   Gets the categories collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of categories.
	/// </value>
	IMongoCollection<Category> Categories { get; }

	/// <summary>
	///   Gets the comments collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of comments.
	/// </value>
	IMongoCollection<Comment> Comments { get; }

	/// <summary>
	///   Gets the statuses collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of statuses.
	/// </value>
	IMongoCollection<Status> Statuses { get; }

	/// <summary>
	///   Gets the users collection.
	/// </summary>
	/// <value>
	///   The MongoDB collection of users.
	/// </value>
	IMongoCollection<User> Users { get; }

	/// <summary>
	///   Gets the MongoDB database instance.
	/// </summary>
	/// <value>
	///   The MongoDB database instance.
	/// </value>
	IMongoDatabase Database { get; }
}
