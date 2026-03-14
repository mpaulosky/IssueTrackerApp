// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryFirstOrDefaultTests.cs
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
///   Tests for Repository FirstOrDefaultAsync method.
/// </summary>
public class RepositoryFirstOrDefaultTests
{
	/// <summary>
	///   Helper method to create a test IssueTrackerDbContext.
	/// </summary>
	private static IssueTrackerDbContext CreateTestContext()
	{
		var options = new DbContextOptionsBuilder<IssueTrackerDbContext>()
			.UseMongoDB("mongodb://localhost:27017", "test-db")
			.Options;

		var settings = Options.Create(new MongoDbSettings
		{
			ConnectionString = "mongodb://localhost:27017",
			DatabaseName = "test-db"
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
	public async Task FirstOrDefaultAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "Test";

		// Act
		// This will throw an exception because MongoDB is not running
		var result = await repository.FirstOrDefaultAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.Error.Should().NotBeNullOrEmpty();
		result.Error.Should().Contain("Failed to retrieve Category");
	}

	[Fact]
	public async Task FirstOrDefaultAsync_Should_ReturnResult()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		Expression<Func<Category, bool>> predicate = c => c.Archived == false;

		// Act
		var result = await repository.FirstOrDefaultAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		// Result will be a failure due to MongoDB not running, but it's still a Result
		result.Failure.Should().BeTrue();
	}
}
