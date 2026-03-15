// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryAnyTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository AnyAsync method.
/// </summary>
public class RepositoryAnyTests : RepositoryTestBase<Category>
{
	[Fact]
	public async Task AnyAsync_WithNoMatchingData_Should_ReturnFalse()
	{
		// Arrange
		var testData = new List<Category>
		{
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Category1" },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Category2" }
		};
		SetupDbSetWithData(testData);
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "NonExistent";

		// Act
		var result = await Sut.AnyAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().BeFalse();
	}

	[Fact]
	public async Task AnyAsync_WithMatchingData_Should_ReturnTrue()
	{
		// Arrange
		var testData = new List<Category>
		{
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Category1" },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Category2" }
		};
		SetupDbSetWithData(testData);
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "Category1";

		// Act
		var result = await Sut.AnyAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task AnyAsync_WithEmptyData_Should_ReturnFalse()
	{
		// Arrange
		SetupEmptyDbSet();
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "Test";

		// Act
		var result = await Sut.AnyAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().BeFalse();
	}

	[Fact]
	public async Task AnyAsync_WithNullPredicate_Should_CheckAnyData()
	{
		// Arrange
		var testData = new List<Category>
		{
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Category1" }
		};
		SetupDbSetWithData(testData);

		// Act - EF Core doesn't allow null predicates for AnyAsync
#pragma warning disable CS8625
		var result = await Sut.AnyAsync(null);
#pragma warning restore CS8625

		// Assert - The operation fails because null predicate is not supported
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Value cannot be null");
	}

	[Fact]
	public async Task AnyAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		SetupDbSetToThrow(new Exception("Database error"));
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "Test";

		// Act
		var result = await Sut.AnyAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to check Category existence");
		VerifyErrorLogged();
	}
}
