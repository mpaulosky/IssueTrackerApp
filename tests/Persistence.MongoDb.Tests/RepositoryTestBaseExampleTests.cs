// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryTestBaseExampleTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Domain.Models;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Example tests demonstrating how to use RepositoryTestBase for unit testing.
/// </summary>
public class RepositoryTestBaseExampleTests : RepositoryTestBase<Category>
{
	[Fact]
	public async Task GetAllAsync_WithEmptyDbSet_Should_ReturnEmptyCollection()
	{
		// Arrange
		SetupEmptyDbSet();
		SetupSaveChangesAsync();

		// Act
		var result = await Sut.GetAllAsync();

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllAsync_WithData_Should_ReturnAllEntities()
	{
		// Arrange
		var categories = new List<Category>
		{
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Bug", CategoryDescription = "Bug reports" },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Feature", CategoryDescription = "Feature requests" }
		};

		SetupDbSetWithData(categories);
		SetupSaveChangesAsync();

		// Act
		var result = await Sut.GetAllAsync();

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value.Should().BeEquivalentTo(categories);
	}

	[Fact]
	public async Task GetByIdAsync_WithValidId_Should_ReturnEntity()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var category = new Category
		{
			Id = id,
			CategoryName = "Bug",
			CategoryDescription = "Bug reports"
		};

		SetupDbSetWithFind(new[] { category }, c => c.Id);
		SetupSaveChangesAsync();

		// Act
		var result = await Sut.GetByIdAsync(id.ToString());

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(category);
	}

	[Fact]
	public async Task AddAsync_WithValidEntity_Should_SucceedAndSaveChanges()
	{
		// Arrange
		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Bug",
			CategoryDescription = "Bug reports"
		};

		SetupEmptyDbSet();
		SetupSaveChangesAsync();

		// Act
		var result = await Sut.AddAsync(category);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(category);
		VerifySaveChangesCalledOnce();
		VerifyInformationLogged();
	}

	[Fact]
	public async Task AddAsync_WhenSaveChangesFails_Should_ReturnFail()
	{
		// Arrange
		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Bug",
			CategoryDescription = "Bug reports"
		};

		var exception = new InvalidOperationException("Database error");
		SetupEmptyDbSet();
		SetupSaveChangesToThrow(exception);

		// Act
		var result = await Sut.AddAsync(category);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to add");
		VerifyErrorLogged();
	}

	/// <summary>
	///   NOTE: Tests for CountAsync, AnyAsync, and FirstOrDefaultAsync with predicates
	///   require more complex mocking setup due to EF Core's async extension methods.
	///   For full integration testing of these methods, consider using TestContainers
	///   with a real MongoDB instance. The infrastructure created here (MockDbSetHelper
	///   and RepositoryTestBase) provides the foundation for unit testing basic CRUD
	///   operations that don't rely on complex async LINQ queries.
	/// </summary>
	[Fact]
	public void Infrastructure_CreatesProperMocks_ForBasicCrudOperations()
	{
		// Arrange & Act
		SetupEmptyDbSet();

		// Assert
		MockContext.Should().NotBeNull();
		MockDbSet.Should().NotBeNull();
		MockLogger.Should().NotBeNull();
		Sut.Should().NotBeNull();
	}
}
