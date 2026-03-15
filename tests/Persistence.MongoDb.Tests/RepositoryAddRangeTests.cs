// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryAddRangeTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository AddRangeAsync method.
/// </summary>
public class RepositoryAddRangeTests
{
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

	[Fact]
	public async Task AddRangeAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var categories = new List<Category>
		{
			new() { CategoryName = "Category 1", CategoryDescription = "Description 1" },
			new() { CategoryName = "Category 2", CategoryDescription = "Description 2" },
			new() { CategoryName = "Category 3", CategoryDescription = "Description 3" }
		};

		// Act
		var result = await repository.AddRangeAsync(categories);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to add Category entities");
	}

	[Fact]
	public async Task AddRangeAsync_Should_ReturnResultType()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var categories = new List<Category>
		{
			new() { CategoryName = "Category 1", CategoryDescription = "Description 1" }
		};

		// Act
		var result = await repository.AddRangeAsync(categories);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeOfType<Result<IEnumerable<Category>>>();
	}

	[Fact]
	public async Task AddRangeAsync_WithEmptyCollection_Should_ReturnSuccess()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var categories = new List<Category>();

		// Act
		var result = await repository.AddRangeAsync(categories);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeOfType<Result<IEnumerable<Category>>>();
	}
}
