// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbFixture.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.Integration;

/// <summary>
///   Shared test fixture using Testcontainers.MongoDb.
///   Starts a MongoDB container as a single-node replica set for transaction support.
/// </summary>
public sealed class MongoDbFixture : IAsyncLifetime
{
	private MongoDbContainer? _container;

	/// <summary>
	///   Gets the MongoDB connection string for the test container.
	/// </summary>
	public string ConnectionString => _container?.GetConnectionString()
		?? throw new InvalidOperationException("MongoDB container is not initialized.");

	/// <summary>
	///   Initializes the MongoDB test container with replica set support.
	/// </summary>
	public async Task InitializeAsync()
	{
		// MongoDB EF Core provider requires a replica set for transactions
		_container = new MongoDbBuilder("mongo:7.0")
			.WithCommand("mongod", "--replSet", "rs0", "--bind_ip_all")
			.WithName($"mongodb-integration-test-{Guid.NewGuid():N}")
			.Build();

		await _container.StartAsync();

		// Initialize single-node replica set (required for SaveChangesAsync transactions)
		await _container.ExecScriptAsync(
			"rs.initiate({_id:'rs0', members:[{_id:0, host:'localhost:27017'}]})");

		// Wait for replica set to elect primary
		await Task.Delay(3000);
	}

	/// <summary>
	///   Creates a new IssueTrackerDbContext with a unique database name.
	/// </summary>
	/// <param name="dbName">Optional database name. If null, a unique name is generated.</param>
	/// <returns>A configured IssueTrackerDbContext instance.</returns>
	public IssueTrackerDbContext CreateDbContext(string? dbName = null)
	{
		var databaseName = dbName ?? $"test-{Guid.NewGuid():N}";
		var settings = Options.Create(new MongoDbSettings
		{
			ConnectionString = ConnectionString,
			DatabaseName = databaseName
		});

		var options = new DbContextOptionsBuilder<IssueTrackerDbContext>()
			.UseMongoDB(ConnectionString, databaseName)
			.Options;

		return new IssueTrackerDbContext(options, settings);
	}

	/// <summary>
	///   Creates a Repository for the specified entity type.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="context">The database context.</param>
	/// <returns>A configured Repository instance.</returns>
	public Repository<TEntity> CreateRepository<TEntity>(IssueTrackerDbContext context) where TEntity : class
	{
		var logger = NullLogger<Repository<TEntity>>.Instance;
		return new Repository<TEntity>(context, logger);
	}

	/// <summary>
	///   Disposes of the MongoDB test container.
	/// </summary>
	public async Task DisposeAsync()
	{
		if (_container is not null)
		{
			await _container.StopAsync();
			await _container.DisposeAsync();
		}
	}
}
