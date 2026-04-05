// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CustomWebApplicationFactory.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

using Persistence.MongoDb;
using Persistence.MongoDb.Configurations;

namespace Web.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that configures the test environment with:
/// - MongoDB Testcontainer for local development, OR
/// - External MongoDB service when MONGODB_CONNECTION_STRING env var is set (CI)
/// - Mock Auth0 authentication using TestAuthHandler
/// - Proper lifecycle management via IAsyncLifetime
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
	private MongoDbContainer? _mongoContainer;
	private string? _connectionString;

	/// <summary>
	/// Indicates whether we're using an external MongoDB service (e.g., CI) instead of Testcontainers.
	/// </summary>
	private bool UseExternalMongoDB => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING"));

	/// <summary>
	/// Gets the MongoDB connection string for the test container or external service.
	/// </summary>
	public string MongoConnectionString => _connectionString
		?? throw new InvalidOperationException("MongoDB is not initialized.");

	/// <summary>
	/// Gets the test database name.
	/// </summary>
	public string DatabaseName => "issuetracker-test-db";

	/// <summary>
	/// Initializes the MongoDB test container or uses external connection.
	/// </summary>
	public async Task InitializeAsync()
	{
		var externalConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");

		if (!string.IsNullOrEmpty(externalConnectionString))
		{
			// Use the external MongoDB service provided by CI environment
			_connectionString = externalConnectionString;
			return;
		}

		// Use Testcontainers for local development
		// EF Core MongoDB provider requires a replica set for transactions
		// TestContainers defaults to no authentication, which is what we want for tests
		_mongoContainer = new MongoDbBuilder("mongo:7.0")
			.WithReplicaSet("rs0")
			.Build();

		await _mongoContainer.StartAsync();

		// Wait for replica set to elect primary
		await Task.Delay(5000);

		_connectionString = _mongoContainer.GetConnectionString();
	}

	/// <summary>
	/// Disposes of the MongoDB test container if one was created.
	/// </summary>
	public new async Task DisposeAsync()
	{
		if (_mongoContainer is not null)
		{
			await _mongoContainer.StopAsync();
			await _mongoContainer.DisposeAsync();
		}

		await base.DisposeAsync();
	}

	/// <summary>
	/// Configures the web host for integration testing.
	/// </summary>
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("IntegrationTesting");

		builder.ConfigureAppConfiguration((_, configBuilder) =>
		{
			// Override MongoDB settings with test container or external service connection
			var testConfig = new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = MongoConnectionString,
				["MongoDB:DatabaseName"] = DatabaseName,
				// Aspire AddMongoDBClient("mongodb") reads this key; without it, service discovery hangs
				["ConnectionStrings:mongodb"] = MongoConnectionString,
				// Auth0 settings (not used since we mock auth, but required for startup)
				["Auth0:Domain"] = "test.auth0.com",
				["Auth0:ClientId"] = "test-client-id",
				["Auth0:ClientSecret"] = "test-client-secret"
			};

			configBuilder.AddInMemoryCollection(testConfig);
		});

		builder.ConfigureTestServices(services =>
		{
			// Remove the real Auth0 authentication
			services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();

			// Add test authentication handler
			services.AddTestAuthentication();

			// Configure authorization to use test scheme
			services.AddAuthorization(options =>
			{
				options.AddPolicy("AdminPolicy", policy =>
					policy.RequireRole("Admin"));
				options.AddPolicy("UserPolicy", policy =>
					policy.RequireRole("User"));
			});

			// Re-register MongoDB settings with test container values
			services.RemoveAll<IOptions<MongoDbSettings>>();
			services.RemoveAll<IOptionsSnapshot<MongoDbSettings>>();
			services.RemoveAll<IOptionsMonitor<MongoDbSettings>>();

			// Capture connection string and database name for closure
			var connectionString = MongoConnectionString;
			var databaseName = DatabaseName;

			services.AddSingleton<IOptions<MongoDbSettings>>(sp =>
				Options.Create(new MongoDbSettings
				{
					ConnectionString = connectionString,
					DatabaseName = databaseName
				}));
		});
	}

	/// <summary>
	/// Creates a new IssueTrackerDbContext for direct database access in tests.
	/// </summary>
	public IssueTrackerDbContext CreateDbContext()
	{
		var scope = Services.CreateScope();
		return scope.ServiceProvider.GetRequiredService<IssueTrackerDbContext>();
	}

	/// <summary>
	/// Clears all data from the test database and in-memory caches.
	/// Uses the MongoDB driver to delete all documents from each collection,
	/// avoiding both EF Core deserialization issues and DropDatabase race conditions
	/// when test classes run with shared fixtures.
	/// </summary>
	public async Task ClearDatabaseAsync()
	{
		var client = new MongoClient(MongoConnectionString);
		var database = client.GetDatabase(DatabaseName);
		var collectionNames = await (await database.ListCollectionNamesAsync()).ToListAsync();

		foreach (var name in collectionNames)
		{
			await database.GetCollection<MongoDB.Bson.BsonDocument>(name)
				.DeleteManyAsync(MongoDB.Driver.FilterDefinition<MongoDB.Bson.BsonDocument>.Empty);
		}

		// Clear in-memory caches to prevent stale analytics data between tests
		if (Services.GetService<IMemoryCache>() is MemoryCache mc)
		{
			mc.Compact(1.0);
		}

		// Clear distributed cache (MemoryDistributedCache wraps an internal MemoryCache)
		if (Services.GetService<IDistributedCache>() is { } dc)
		{
			// Remove analytics cache keys with null date parameters (most common in tests)
			await dc.RemoveAsync("analytics_summary__");
			await dc.RemoveAsync("analytics_status__");
			await dc.RemoveAsync("analytics_category__");
			await dc.RemoveAsync("analytics_overtime__");
			await dc.RemoveAsync("analytics_resolution__");

			// Remove reference-data cache keys added in Sprint 1
			await dc.RemoveAsync("categories_list_False");
			await dc.RemoveAsync("categories_list_True");
			await dc.RemoveAsync("statuses_list_False");
			await dc.RemoveAsync("statuses_list_True");
			await dc.RemoveAsync("lookup_categories");
			await dc.RemoveAsync("lookup_statuses");

			// Bump the issues version counter so all previously cached paginated
			// list pages (keyed by version number) become orphaned and will not
			// be served to the next test in the same factory instance.
			await using var scope = Services.CreateAsyncScope();
			var cacheHelper = scope.ServiceProvider.GetRequiredService<Web.Services.DistributedCacheHelper>();
			await cacheHelper.BumpVersionAsync("issues_version");
		}
	}
}
