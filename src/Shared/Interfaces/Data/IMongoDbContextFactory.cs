// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IMongoDbContextFactory.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =======================================================

namespace Shared.Interfaces.Data;

/// <summary>
///   Abstraction for a design-time factory that can create an <see cref="IMongoDbContext" />.
///   This allows the factory to be injected or mocked in tests if needed.
/// </summary>
public interface IMongoDbContextFactory
{

	/// <summary>
	///   Create an instance of <see cref="IMongoDbContext" /> for the provided args.
	/// </summary>
	/// <returns>A new <see cref="IMongoDbContext" />.</returns>
	IMongoDbContext CreateDbContext();

	/// <summary>
	///   Gets the MongoDB database instance from the factory.
	/// </summary>
	IMongoDatabase Database { get; }

}
