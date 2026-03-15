// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DbContextIntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.Integration;

/// <summary>
///   Integration tests for IssueTrackerDbContext operations.
/// </summary>
[Collection("MongoDb")]
public sealed class DbContextIntegrationTests
{
	private readonly MongoDbFixture _fixture;

	public DbContextIntegrationTests(MongoDbFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task InitializeDatabaseAsync_Should_CreateDatabase()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();

		// Act
		await context.InitializeDatabaseAsync();

		// Assert - No exception thrown means success
		context.Should().NotBeNull();
		context.Database.Should().NotBeNull();
	}

	[Fact]
	public async Task Issues_DbSet_Should_BeAccessible()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();

		// Act
		var dbSet = context.Issues;
		var count = await dbSet.CountAsync();

		// Assert
		dbSet.Should().NotBeNull();
		count.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public async Task Categories_DbSet_Should_BeAccessible()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();

		// Act
		var dbSet = context.Categories;
		var count = await dbSet.CountAsync();

		// Assert
		dbSet.Should().NotBeNull();
		count.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public async Task Statuses_DbSet_Should_BeAccessible()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();

		// Act
		var dbSet = context.Statuses;
		var count = await dbSet.CountAsync();

		// Assert
		dbSet.Should().NotBeNull();
		count.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public async Task Comments_DbSet_Should_BeAccessible()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();

		// Act
		var dbSet = context.Comments;
		var count = await dbSet.CountAsync();

		// Assert
		dbSet.Should().NotBeNull();
		count.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public async Task Attachments_DbSet_Should_BeAccessible()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();

		// Act
		var dbSet = context.Attachments;
		var count = await dbSet.CountAsync();

		// Assert
		dbSet.Should().NotBeNull();
		count.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public async Task EmailQueue_DbSet_Should_BeAccessible()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();

		// Act
		var dbSet = context.EmailQueue;
		var count = await dbSet.CountAsync();

		// Assert
		dbSet.Should().NotBeNull();
		count.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public async Task OnModelCreating_Should_ApplyConfigurations()
	{
		// Arrange
		await using var context = _fixture.CreateDbContext();
		await context.InitializeDatabaseAsync();

		// Act - Create and save a Category entity to verify model configuration
		var category = new Category
		{
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		await context.Categories.AddAsync(category);
		await context.SaveChangesAsync();

		// Assert - Retrieve the saved entity
		var savedCategory = await context.Categories
			.FirstOrDefaultAsync(c => c.CategoryName == "Test Category");

		savedCategory.Should().NotBeNull();
		savedCategory!.Id.Should().NotBe(ObjectId.Empty);
		savedCategory.CategoryName.Should().Be("Test Category");
	}
}
