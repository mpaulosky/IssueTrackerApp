// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryWriteIntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.Integration;

/// <summary>
///   Integration tests for Repository write operations.
/// </summary>
[Collection("MongoDb")]
public sealed class RepositoryWriteIntegrationTests
{
	private readonly MongoDbFixture _fixture;

	public RepositoryWriteIntegrationTests(MongoDbFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task AddAsync_Should_PersistEntity()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var category = new Category
		{
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		// Act
		var addResult = await repository.AddAsync(category);
		var categoryId = addResult.Value!.Id.ToString();
		var getResult = await repository.GetByIdAsync(categoryId);

		// Assert
		addResult.Success.Should().BeTrue();
		getResult.Success.Should().BeTrue();
		getResult.Value.Should().NotBeNull();
		getResult.Value!.CategoryName.Should().Be("Test Category");
		getResult.Value.CategoryDescription.Should().Be("Test Description");
	}

	[Fact]
	public async Task AddAsync_Should_ReturnAddedEntity()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var category = new Category
		{
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		// Act
		var result = await repository.AddAsync(category);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().NotBe(ObjectId.Empty);
		result.Value.CategoryName.Should().Be("Test Category");
		result.Value.CategoryDescription.Should().Be("Test Description");
	}

	[Fact]
	public async Task AddRangeAsync_Should_PersistMultipleEntities()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var categories = new[]
		{
			new Category { CategoryName = "Category 1", CategoryDescription = "Description 1" },
			new Category { CategoryName = "Category 2", CategoryDescription = "Description 2" },
			new Category { CategoryName = "Category 3", CategoryDescription = "Description 3" }
		};

		// Act
		var addResult = await repository.AddRangeAsync(categories);
		var getAllResult = await repository.GetAllAsync();

		// Assert
		addResult.Success.Should().BeTrue();
		getAllResult.Success.Should().BeTrue();
		getAllResult.Value.Should().HaveCount(3);
		getAllResult.Value.Should().Contain(c => c.CategoryName == "Category 1");
		getAllResult.Value.Should().Contain(c => c.CategoryName == "Category 2");
		getAllResult.Value.Should().Contain(c => c.CategoryName == "Category 3");
	}

	[Fact]
	public async Task AddRangeAsync_Should_ReturnAllEntities()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var categories = new[]
		{
			new Category { CategoryName = "Category 1", CategoryDescription = "Description 1" },
			new Category { CategoryName = "Category 2", CategoryDescription = "Description 2" },
			new Category { CategoryName = "Category 3", CategoryDescription = "Description 3" }
		};

		// Act
		var result = await repository.AddRangeAsync(categories);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(3);
		result.Value.All(c => c.Id != ObjectId.Empty).Should().BeTrue();
	}

	[Fact]
	public async Task UpdateAsync_Should_ModifyExistingEntity()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var category = new Category
		{
			CategoryName = "Original Name",
			CategoryDescription = "Original Description"
		};

		var addResult = await repository.AddAsync(category);
		var addedCategory = addResult.Value!;

		// Modify the category
		addedCategory.CategoryName = "Updated Name";
		addedCategory.CategoryDescription = "Updated Description";

		// Act
		var updateResult = await repository.UpdateAsync(addedCategory);
		var getResult = await repository.GetByIdAsync(addedCategory.Id.ToString());

		// Assert
		updateResult.Success.Should().BeTrue();
		getResult.Success.Should().BeTrue();
		getResult.Value.Should().NotBeNull();
		getResult.Value!.CategoryName.Should().Be("Updated Name");
		getResult.Value.CategoryDescription.Should().Be("Updated Description");
	}

	[Fact]
	public async Task DeleteAsync_Should_RemoveEntity()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var category = new Category
		{
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		var addResult = await repository.AddAsync(category);
		var categoryId = addResult.Value!.Id.ToString();

		// Act
		var deleteResult = await repository.DeleteAsync(categoryId);
		var getResult = await repository.GetByIdAsync(categoryId);

		// Assert
		deleteResult.Success.Should().BeTrue();
		deleteResult.Value.Should().BeTrue();
		getResult.Success.Should().BeFalse();
		getResult.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task DeleteAsync_WithNonExistentId_Should_ReturnNotFoundError()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);
		var nonExistentId = ObjectId.GenerateNewId().ToString();

		// Act
		var result = await repository.DeleteAsync(nonExistentId);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Contain("was not found");
	}

	[Fact]
	public async Task ConcurrentAdds_Should_NotConflict()
	{
		// Arrange - Use a shared database name for all contexts in this test
		var sharedDbName = $"concurrent-test-{Guid.NewGuid():N}";
		
		// Initialize database once before concurrent operations
		await using var initContext = _fixture.CreateDbContext(sharedDbName);
		await initContext.InitializeDatabaseAsync();

		// Each concurrent task gets its own DbContext pointing to the SAME database
		// to avoid SaveChangesAsync race conditions while sharing data.
		var tasks = Enumerable.Range(1, 5).Select(async i =>
		{
			await using var context = _fixture.CreateDbContext(sharedDbName);
			var repository = _fixture.CreateRepository<Category>(context);
			var category = new Category
			{
				CategoryName = $"Concurrent Category {i}",
				CategoryDescription = $"Description {i}"
			};
			return await repository.AddAsync(category);
		});

		// Act
		var results = await Task.WhenAll(tasks);

		// Assert
		results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
		results.Should().OnlyHaveUniqueItems(r => r.Value!.Id);

		// Verify all entities were persisted with a fresh context pointing to same db
		await using var verifyContext = _fixture.CreateDbContext(sharedDbName);
		var repository = _fixture.CreateRepository<Category>(verifyContext);
		var getAllResult = await repository.GetAllAsync();
		getAllResult.Success.Should().BeTrue();
		getAllResult.Value.Should().HaveCount(5);
	}
}
