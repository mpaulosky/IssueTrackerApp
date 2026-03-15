// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryCountTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository CountAsync method.
/// </summary>
public class RepositoryCountTests : RepositoryTestBase<Category>
{
	[Fact]
	public async Task CountAsync_WithMatchingData_Should_ReturnCorrectCount()
	{
		// Arrange
		var testData = new List<Category>
		{
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Test1" },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Test2" },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Other" }
		};
		SetupDbSetWithData(testData);
		Expression<Func<Category, bool>> predicate = c => c.CategoryName!.StartsWith("Test");

		// Act
		var result = await Sut.CountAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().Be(2);
	}

	[Fact]
	public async Task CountAsync_WithNoMatchingData_Should_ReturnZero()
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
		var result = await Sut.CountAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().Be(0);
	}

	[Fact]
	public async Task CountAsync_WithEmptyData_Should_ReturnZero()
	{
		// Arrange
		SetupEmptyDbSet();
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "Test";

		// Act
		var result = await Sut.CountAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().Be(0);
	}

	[Fact]
	public async Task CountAsync_WithNullPredicate_Should_ReturnTotalCount()
	{
		// Arrange
		var testData = new List<Category>
		{
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Category1" },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Category2" },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Category3" }
		};
		SetupDbSetWithData(testData);

		// Act
#pragma warning disable CS8625
		var result = await Sut.CountAsync(null);
#pragma warning restore CS8625

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().Be(3);
	}

	[Fact]
	public async Task CountAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		SetupDbSetToThrow(new Exception("Database error"));
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "Test";

		// Act
		var result = await Sut.CountAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to count Category entities");
		VerifyErrorLogged();
	}
}
