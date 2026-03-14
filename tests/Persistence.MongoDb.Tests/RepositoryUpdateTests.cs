// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryUpdateTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository UpdateAsync method.
/// </summary>
public class RepositoryUpdateTests
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
	public async Task UpdateAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Updated Category",
			CategoryDescription = "Updated Description"
		};

		// Act
		var result = await repository.UpdateAsync(category);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to update Category");
	}

	[Fact]
	public async Task UpdateAsync_Should_ReturnResultType()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Updated Category",
			CategoryDescription = "Updated Description"
		};

		// Act
		var result = await repository.UpdateAsync(category);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeOfType<Result<Category>>();
	}
}
