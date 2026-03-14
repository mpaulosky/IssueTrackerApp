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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
		_mongoContainer = new MongoDbBuilder("mongo:7.0")
			.WithCommand("mongod", "--replSet", "rs0", "--bind_ip_all")
			.WithName($"mongodb-integration-test-{Guid.NewGuid():N}")
			.Build();

		await _mongoContainer.StartAsync();

		// Initialize single-node replica set (required for SaveChangesAsync transactions)
		await _mongoContainer.ExecScriptAsync(
			"rs.initiate({_id:'rs0', members:[{_id:0, host:'localhost:27017'}]})");

		// Wait for replica set to elect primary
		await Task.Delay(3000);

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
		builder.UseEnvironment("Testing");

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
	/// Clears all data from the test database.
	/// </summary>
	public async Task ClearDatabaseAsync()
	{
		await using var context = CreateDbContext();

		// Remove all entities from collections
		context.Issues.RemoveRange(context.Issues);
		context.Categories.RemoveRange(context.Categories);
		context.Statuses.RemoveRange(context.Statuses);
		context.Comments.RemoveRange(context.Comments);
		context.Attachments.RemoveRange(context.Attachments);
		context.EmailQueue.RemoveRange(context.EmailQueue);

		await context.SaveChangesAsync();
	}
}
