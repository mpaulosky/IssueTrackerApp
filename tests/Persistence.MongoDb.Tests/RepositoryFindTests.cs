// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryFindTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Domain.Models;

using Microsoft.Extensions.Logging;

using Persistence.MongoDb.Repositories;
using Persistence.MongoDb.Tests.Helpers;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository FindAsync method.
/// </summary>
public class RepositoryFindTests
{
	[Fact]
	public async Task FindAsync_Should_ReturnEmptyList_WhenNoMatches()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockContext.Set<Category>().Returns(mockDbSet);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);
		Expression<Func<Category, bool>> predicate = c => c.CategoryName == "Test";

		// Act
		var result = await repository.FindAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task FindAsync_Should_ReturnMatchingItems_WhenPredicateMatches()
	{
		// Arrange
		var categories = new List<Category>
		{
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Test1", Archived = false },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Test2", Archived = false },
			new() { Id = ObjectId.GenerateNewId(), CategoryName = "Test3", Archived = true }
		};

		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet(categories);
		mockContext.Set<Category>().Returns(mockDbSet);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);
		Expression<Func<Category, bool>> predicate = c => c.Archived == false;

		// Act
		var result = await repository.FindAsync(predicate);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(2);
		result.Value.Should().OnlyContain(c => c.Archived == false);
	}
}
