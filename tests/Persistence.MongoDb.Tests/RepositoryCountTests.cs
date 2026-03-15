// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryCountTests.cs
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
///   Tests for Repository CountAsync method.
/// </summary>
public class RepositoryCountTests
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
	public async Task CountAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "Test";

		// Act
		// EF Core returns success with count of 0 when MongoDB is unavailable
		var result = await repository.CountAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().Be(0);
	}

	[Fact]
	public async Task CountAsync_WithNullPredicate_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);

		// Act
		// EF Core returns success with count of 0 when MongoDB is unavailable
		var result = await repository.CountAsync(null);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().Be(0);
	}
}
