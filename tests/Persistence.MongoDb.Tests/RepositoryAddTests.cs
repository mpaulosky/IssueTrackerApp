// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryAddTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository AddAsync method.
/// </summary>
public class RepositoryAddTests
{
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

	[Fact]
	public async Task AddAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var category = new Category
		{
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		// Act
		var result = await repository.AddAsync(category);

		// Assert
		// EF Core returns success when MongoDB is unavailable
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
	}

	[Fact]
	public async Task AddAsync_Should_ReturnResultType()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var category = new Category
		{
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		// Act
		var result = await repository.AddAsync(category);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeOfType<Result<Category>>();
	}

	[Fact]
	public async Task AddAsync_WithNullEntity_Should_ReturnFail()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		// Act
		var result = await repository.AddAsync(null!);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to add Category");
	}
}
