// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DataSeeder.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Web.Data;

/// <summary>
///   Service for seeding default data into the database.
/// </summary>
public interface IDataSeeder
{
	/// <summary>
	///   Seeds default categories and statuses if they don't exist.
	/// </summary>
	Task SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of IDataSeeder.
/// </summary>
public sealed class DataSeeder : IDataSeeder
{
	private readonly IRepository<Category> _categoryRepository;
	private readonly IRepository<Status> _statusRepository;
	private readonly ILogger<DataSeeder> _logger;

	public DataSeeder(
		IRepository<Category> categoryRepository,
		IRepository<Status> statusRepository,
		ILogger<DataSeeder> logger)
	{
		_categoryRepository = categoryRepository;
		_statusRepository = statusRepository;
		_logger = logger;
	}

	public async Task SeedAsync(CancellationToken cancellationToken = default)
	{
		await SeedCategoriesAsync(cancellationToken);
		await SeedStatusesAsync(cancellationToken);
	}

	private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
	{
		var existingCategories = await _categoryRepository.GetAllAsync(cancellationToken);

		if (existingCategories.Success && existingCategories.Value?.Any() == true)
		{
			_logger.LogInformation("Categories already seeded, skipping...");
			return;
		}

		_logger.LogInformation("Seeding default categories...");

		var defaultCategories = new List<Category>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Bug",
				CategoryDescription = "Something isn't working as expected",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Feature",
				CategoryDescription = "New feature or request",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Enhancement",
				CategoryDescription = "Improvement to existing functionality",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Question",
				CategoryDescription = "Further information is requested",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Documentation",
				CategoryDescription = "Improvements or additions to documentation",
				DateCreated = DateTime.UtcNow
			}
		};

		var result = await _categoryRepository.AddRangeAsync(defaultCategories, cancellationToken);

		if (result.Success)
		{
			_logger.LogInformation("Successfully seeded {Count} categories", defaultCategories.Count);
		}
		else
		{
			_logger.LogError("Failed to seed categories: {Error}", result.Error);
		}
	}

	private async Task SeedStatusesAsync(CancellationToken cancellationToken)
	{
		var existingStatuses = await _statusRepository.GetAllAsync(cancellationToken);

		if (existingStatuses.Success && existingStatuses.Value?.Any() == true)
		{
			_logger.LogInformation("Statuses already seeded, skipping...");
			return;
		}

		_logger.LogInformation("Seeding default statuses...");

		var defaultStatuses = new List<Status>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Open",
				StatusDescription = "Issue is open and awaiting review",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "In Progress",
				StatusDescription = "Issue is currently being worked on",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Under Review",
				StatusDescription = "Issue is under review",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Resolved",
				StatusDescription = "Issue has been resolved",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Closed",
				StatusDescription = "Issue has been closed",
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Won't Fix",
				StatusDescription = "Issue will not be addressed",
				DateCreated = DateTime.UtcNow
			}
		};

		var result = await _statusRepository.AddRangeAsync(defaultStatuses, cancellationToken);

		if (result.Success)
		{
			_logger.LogInformation("Successfully seeded {Count} statuses", defaultStatuses.Count);
		}
		else
		{
			_logger.LogError("Failed to seed statuses: {Error}", result.Error);
		}
	}
}

/// <summary>
///   Extension methods for registering the DataSeeder.
/// </summary>
public static class DataSeederExtensions
{
	/// <summary>
	///   Adds the DataSeeder to the service collection.
	/// </summary>
	public static IServiceCollection AddDataSeeder(this IServiceCollection services)
	{
		services.AddScoped<IDataSeeder, DataSeeder>();
		return services;
	}

	/// <summary>
	///   Seeds the default data.
	/// </summary>
	public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();
		var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
		await seeder.SeedAsync();
	}
}
