// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryAddRangeTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Persistence.MongoDb.Tests.Helpers;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository AddRangeAsync method.
/// </summary>
public class RepositoryAddRangeTests
{
	[Fact]
	public async Task AddRangeAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<int>(new Exception("Database error")));
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

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
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

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
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

		var categories = new List<Category>();

		// Act
		var result = await repository.AddRangeAsync(categories);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeOfType<Result<IEnumerable<Category>>>();
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task AddRangeAsync_WithValidEntities_Should_ReturnSuccess()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(3);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

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
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(3);
	}
}
