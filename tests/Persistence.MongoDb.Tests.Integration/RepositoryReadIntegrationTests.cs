// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryReadIntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.Integration;

/// <summary>
///   Integration tests for Repository read operations.
/// </summary>
[Collection("MongoDb")]
public sealed class RepositoryReadIntegrationTests
{
	private readonly MongoDbFixture _fixture;

	public RepositoryReadIntegrationTests(MongoDbFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task GetByIdAsync_WithExistingEntity_Should_ReturnSuccess()
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
		var result = await repository.GetByIdAsync(categoryId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.ToString().Should().Be(categoryId);
		result.Value.CategoryName.Should().Be("Test Category");
	}

	[Fact]
	public async Task GetByIdAsync_WithNonExistentId_Should_ReturnNotFoundError()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);
		var nonExistentId = ObjectId.GenerateNewId().ToString();

		// Act
		var result = await repository.GetByIdAsync(nonExistentId);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Contain("was not found");
	}

	[Fact]
	public async Task GetAllAsync_Should_ReturnAllEntities()
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

		await repository.AddRangeAsync(categories);

		// Act
		var result = await repository.GetAllAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCountGreaterThanOrEqualTo(3);
		result.Value.Should().Contain(c => c.CategoryName == "Category 1");
		result.Value.Should().Contain(c => c.CategoryName == "Category 2");
		result.Value.Should().Contain(c => c.CategoryName == "Category 3");
	}

	[Fact]
	public async Task GetAllAsync_WhenEmpty_Should_ReturnEmptyCollection()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		// Act
		var result = await repository.GetAllAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task FindAsync_WithMatchingPredicate_Should_ReturnFilteredResults()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var categories = new[]
		{
			new Category { CategoryName = "Bug", CategoryDescription = "Bug reports" },
			new Category { CategoryName = "Feature", CategoryDescription = "Feature requests" },
			new Category { CategoryName = "Bug Fix", CategoryDescription = "Bug fixes" }
		};

		await repository.AddRangeAsync(categories);

		// Act
		var result = await repository.FindAsync(c => c.CategoryName.Contains("Bug"));

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(2);
		result.Value.Should().Contain(c => c.CategoryName == "Bug");
		result.Value.Should().Contain(c => c.CategoryName == "Bug Fix");
	}

	[Fact]
	public async Task FindAsync_WithNoMatch_Should_ReturnEmptyCollection()
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

		await repository.AddAsync(category);

		// Act
		var result = await repository.FindAsync(c => c.CategoryName == "NonExistent");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task FirstOrDefaultAsync_WithMatch_Should_ReturnEntity()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var categories = new[]
		{
			new Category { CategoryName = "First", CategoryDescription = "First category" },
			new Category { CategoryName = "Second", CategoryDescription = "Second category" }
		};

		await repository.AddRangeAsync(categories);

		// Act
		var result = await repository.FirstOrDefaultAsync(c => c.CategoryName == "Second");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.CategoryName.Should().Be("Second");
	}

	[Fact]
	public async Task FirstOrDefaultAsync_WithNoMatch_Should_ReturnNullValue()
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

		await repository.AddAsync(category);

		// Act
		var result = await repository.FirstOrDefaultAsync(c => c.CategoryName == "NonExistent");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeNull();
	}

	[Fact]
	public async Task AnyAsync_WhenExists_Should_ReturnTrue()
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

		await repository.AddAsync(category);

		// Act
		var result = await repository.AnyAsync(c => c.CategoryName == "Test Category");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task AnyAsync_WhenNotExists_Should_ReturnFalse()
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

		await repository.AddAsync(category);

		// Act
		var result = await repository.AnyAsync(c => c.CategoryName == "NonExistent");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeFalse();
	}

	[Fact]
	public async Task CountAsync_Should_ReturnCorrectCount()
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

		await repository.AddRangeAsync(categories);

		// Act
		var result = await repository.CountAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be(3);
	}

	[Fact]
	public async Task CountAsync_WithPredicate_Should_ReturnFilteredCount()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();
		var repository = _fixture.CreateRepository<Category>(context);

		var categories = new[]
		{
			new Category { CategoryName = "Bug", CategoryDescription = "Bug reports" },
			new Category { CategoryName = "Feature", CategoryDescription = "Feature requests" },
			new Category { CategoryName = "Bug Fix", CategoryDescription = "Bug fixes" }
		};

		await repository.AddRangeAsync(categories);

		// Act
		var result = await repository.CountAsync(c => c.CategoryName.Contains("Bug"));

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be(2);
	}
}
