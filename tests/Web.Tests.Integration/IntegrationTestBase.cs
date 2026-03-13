// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IntegrationTestBase.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Domain.DTOs;
using Domain.Models;
using MongoDB.Bson;
using Persistence.MongoDb;

namespace Web.Tests.Integration;

/// <summary>
/// Base class for integration tests providing common setup, HttpClient access,
/// and helper methods for authentication and data seeding.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
	/// <summary>
	/// The custom web application factory instance.
	/// </summary>
	protected CustomWebApplicationFactory Factory { get; }

	/// <summary>
	/// The default HttpClient for making requests to the test server.
	/// </summary>
	protected HttpClient Client { get; private set; } = null!;

	/// <summary>
	/// Initializes a new instance of the <see cref="IntegrationTestBase"/> class.
	/// </summary>
	protected IntegrationTestBase(CustomWebApplicationFactory factory)
	{
		Factory = factory;
	}

	/// <summary>
	/// Initializes the test by creating a client and clearing the database.
	/// </summary>
	public virtual async Task InitializeAsync()
	{
		Client = Factory.CreateClient();
		await Factory.ClearDatabaseAsync();
	}

	/// <summary>
	/// Cleans up after the test.
	/// </summary>
	public virtual Task DisposeAsync()
	{
		Client?.Dispose();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Creates an authenticated HttpClient with default User role.
	/// </summary>
	protected HttpClient CreateAuthenticatedClient()
	{
		return CreateAuthenticatedClient("User");
	}

	/// <summary>
	/// Creates an authenticated HttpClient with the specified role.
	/// </summary>
	/// <param name="role">The role to assign to the test user (e.g., "User", "Admin").</param>
	protected HttpClient CreateAuthenticatedClient(string role)
	{
		var client = Factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Role", role);
		return client;
	}

	/// <summary>
	/// Creates an authenticated HttpClient with multiple roles.
	/// </summary>
	/// <param name="roles">The roles to assign to the test user.</param>
	protected HttpClient CreateAuthenticatedClient(params string[] roles)
	{
		var client = Factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Role", string.Join(",", roles));
		return client;
	}

	/// <summary>
	/// Creates an authenticated HttpClient with a specific user ID.
	/// </summary>
	/// <param name="userId">The user ID to use.</param>
	/// <param name="role">The role to assign.</param>
	protected HttpClient CreateAuthenticatedClient(string userId, string role)
	{
		var client = Factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Role", role);
		client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
		return client;
	}

	/// <summary>
	/// Creates an anonymous (unauthenticated) HttpClient.
	/// </summary>
	protected HttpClient CreateAnonymousClient()
	{
		var client = Factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
		return client;
	}

	/// <summary>
	/// Seeds test categories into the database.
	/// </summary>
	/// <returns>The seeded categories.</returns>
	protected async Task<List<Category>> SeedCategoriesAsync()
	{
		await using var context = Factory.CreateDbContext();

		var categories = new List<Category>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Bug",
				CategoryDescription = "Bug report category"
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Feature",
				CategoryDescription = "Feature request category"
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Enhancement",
				CategoryDescription = "Enhancement request category"
			}
		};

		context.Categories.AddRange(categories);
		await context.SaveChangesAsync();

		return categories;
	}

	/// <summary>
	/// Seeds test statuses into the database.
	/// </summary>
	/// <returns>The seeded statuses.</returns>
	protected async Task<List<Status>> SeedStatusesAsync()
	{
		await using var context = Factory.CreateDbContext();

		var statuses = new List<Status>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Open",
				StatusDescription = "Issue is open"
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "In Progress",
				StatusDescription = "Issue is being worked on"
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Closed",
				StatusDescription = "Issue is closed"
			}
		};

		context.Statuses.AddRange(statuses);
		await context.SaveChangesAsync();

		return statuses;
	}

	/// <summary>
	/// Seeds common test data (categories and statuses).
	/// </summary>
	protected async Task<(List<Category> Categories, List<Status> Statuses)> SeedTestDataAsync()
	{
		var categories = await SeedCategoriesAsync();
		var statuses = await SeedStatusesAsync();
		return (categories, statuses);
	}

	/// <summary>
	/// Seeds a test issue into the database.
	/// </summary>
	/// <param name="category">The category for the issue.</param>
	/// <param name="status">The status for the issue.</param>
	/// <param name="title">Optional title override.</param>
	/// <returns>The seeded issue.</returns>
	protected async Task<Issue> SeedIssueAsync(
		Category category,
		Status status,
		string title = "Test Issue")
	{
		await using var context = Factory.CreateDbContext();

		var author = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);

		var issue = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = title,
			Description = "Test issue description",
			Category = new CategoryDto(category),
			Status = new StatusDto(status),
			Author = author
		};

		context.Issues.Add(issue);
		await context.SaveChangesAsync();

		return issue;
	}

	/// <summary>
	/// Seeds multiple test issues into the database.
	/// </summary>
	/// <param name="category">The category for the issues.</param>
	/// <param name="status">The status for the issues.</param>
	/// <param name="count">Number of issues to create.</param>
	/// <returns>The seeded issues.</returns>
	protected async Task<List<Issue>> SeedIssuesAsync(
		Category category,
		Status status,
		int count)
	{
		await using var context = Factory.CreateDbContext();

		var author = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var categoryDto = new CategoryDto(category);
		var statusDto = new StatusDto(status);

		var issues = new List<Issue>();
		for (var i = 1; i <= count; i++)
		{
			issues.Add(new Issue
			{
				Id = ObjectId.GenerateNewId(),
				Title = $"Test Issue {i}",
				Description = $"Test issue description {i}",
				Category = categoryDto,
				Status = statusDto,
				Author = author
			});
		}

		context.Issues.AddRange(issues);
		await context.SaveChangesAsync();

		return issues;
	}

	/// <summary>
	/// Gets a service from the DI container.
	/// </summary>
	/// <typeparam name="T">The service type.</typeparam>
	protected T GetService<T>() where T : notnull
	{
		var scope = Factory.Services.CreateScope();
		return scope.ServiceProvider.GetRequiredService<T>();
	}

	/// <summary>
	/// Creates a new DbContext for direct database access.
	/// </summary>
	protected IssueTrackerDbContext CreateDbContext()
	{
		return Factory.CreateDbContext();
	}
}
