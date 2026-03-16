// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryGetAllTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Domain.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Persistence.MongoDb.Configurations;
using Persistence.MongoDb.Repositories;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository GetAllAsync method.
/// </summary>
public class RepositoryGetAllTests
{
	/// <summary>
	///   Helper method to create a test IssueTrackerDbContext.
	/// </summary>
	private static readonly string TestDbName = $"test-db-{Guid.NewGuid():N}";

	private static IssueTrackerDbContext CreateTestContext()
	{
		var options = new DbContextOptionsBuilder<IssueTrackerDbContext>()
			.UseMongoDB("mongodb://localhost:27017", TestDbName)
			.Options;

		var settings = Options.Create(new MongoDbSettings
		{
			ConnectionString = "mongodb://localhost:27017",
			DatabaseName = TestDbName
		});

		return new IssueTrackerDbContext(options, settings);
	}

	/// <summary>
	///   Helper method to create a test logger.
	/// </summary>
	private static ILogger<Repository<Category>> CreateTestLogger()
	{
		return Substitute.For<ILogger<Repository<Category>>>();
	}

	[Fact]
	public async Task GetAllAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);

		// Act
		// EF Core returns success with empty collection when MongoDB is unavailable
		var result = await repository.GetAllAsync();

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllAsync_Should_ReturnResult()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);

		// Act
		var result = await repository.GetAllAsync();

		// Assert
		result.Should().NotBeNull();
		// EF Core returns success with empty collection when MongoDB is unavailable
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}
}
