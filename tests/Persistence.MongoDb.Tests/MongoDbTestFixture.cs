// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbTestFixture.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Microsoft.Extensions.Options;

using MongoDB.Driver;

using Persistence.MongoDb.Configurations;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Test fixture for MongoDB unit tests that handles database cleanup.
/// </summary>
public sealed class MongoDbTestFixture : IAsyncLifetime
{
	private const string CONNECTION_STRING = "mongodb://localhost:27017";
	private const string DATABASE_NAME = "test-db-unit";
	private IMongoClient? _client;
	private IMongoDatabase? _database;

	/// <summary>
	///   Gets the test database name with a unique suffix for test isolation.
	/// </summary>
	public string TestDatabaseName => $"{DATABASE_NAME}-{Guid.NewGuid():N}";

	/// <summary>
	///   Creates a test IssueTrackerDbContext with a clean database.
	/// </summary>
	public IssueTrackerDbContext CreateTestContext()
	{
		var dbName = TestDatabaseName;
		var options = new DbContextOptionsBuilder<IssueTrackerDbContext>()
			.UseMongoDB(CONNECTION_STRING, dbName)
			.Options;

		var settings = Options.Create(new MongoDbSettings
		{
			ConnectionString = CONNECTION_STRING,
			DatabaseName = dbName
		});

		return new IssueTrackerDbContext(options, settings);
	}

	/// <summary>
	///   Initializes the test fixture - called once before all tests.
	/// </summary>
	public Task InitializeAsync()
	{
		_client = new MongoClient(CONNECTION_STRING);
		_database = _client.GetDatabase(DATABASE_NAME);

		// Ensure database is clean before tests
		return CleanupDatabaseAsync();
	}

	/// <summary>
	///   Disposes the test fixture - called once after all tests.
	/// </summary>
	public Task DisposeAsync()
	{
		// Clean up test database
		var cleanupTask = CleanupDatabaseAsync();
		_client = null;
		_database = null;
		return cleanupTask;
	}

	/// <summary>
	///   Drops all test databases to ensure clean state.
	/// </summary>
	private async Task CleanupDatabaseAsync()
	{
		if (_client is null)
		{
			return;
		}

		try
		{
			// List all databases
			var databasesCursor = await _client.ListDatabaseNamesAsync();
			var databases = await databasesCursor.ToListAsync();
			
			foreach (var dbName in databases)
			{
				// Drop test databases
				if (dbName.StartsWith("test-db", StringComparison.OrdinalIgnoreCase))
				{
					await _client.DropDatabaseAsync(dbName);
				}
			}
		}
		catch
		{
			// Silently ignore cleanup errors - MongoDB might not be running
		}
	}
}

/// <summary>
///   Collection definition for MongoDB unit tests.
/// </summary>
[CollectionDefinition("MongoDb Unit Tests")]
public sealed class MongoDbTestCollection : ICollectionFixture<MongoDbTestFixture>
{
	// This class has no code, and is never created. Its purpose is simply
	// to be the place to apply [CollectionDefinition] and all the
	// ICollectionFixture<> interfaces.
}
