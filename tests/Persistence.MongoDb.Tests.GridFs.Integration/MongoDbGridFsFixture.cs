// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbGridFsFixture.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.GridFs.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.GridFs.Integration;

/// <summary>
///   Shared test fixture using Testcontainers.MongoDb for GridFS integration tests.
///   Starts a MongoDB container as a single-node replica set.
/// </summary>
public sealed class MongoDbGridFsFixture : IAsyncLifetime
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
		_container = new MongoDbBuilder("mongo:7.0")
			.WithReplicaSet("rs0")
			.Build();

		await _container.StartAsync();

		// Wait for replica set to initialize
		await Task.Delay(3000);
	}

	/// <summary>
	///   Creates a new GridFsStorageService backed by an isolated database.
	///   Each call returns a fully independent service instance.
	/// </summary>
	public GridFsStorageService CreateGridFsStorageService()
	{
		var database = new MongoClient(ConnectionString)
			.GetDatabase($"gridfs-test-{Guid.NewGuid():N}");

		return new GridFsStorageService(database, NullLogger<GridFsStorageService>.Instance);
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
