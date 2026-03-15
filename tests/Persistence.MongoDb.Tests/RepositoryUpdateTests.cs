// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryUpdateTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Persistence.MongoDb.Tests.Helpers;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository UpdateAsync method.
/// </summary>
public class RepositoryUpdateTests
{
	[Fact]
	public async Task UpdateAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<int>(new Exception("Database error")));
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

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
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

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

	[Fact]
	public async Task UpdateAsync_WithValidEntity_Should_ReturnSuccess()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

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
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().Be(category);
	}

	[Fact]
	public async Task UpdateAsync_WithNullEntity_Should_ReturnFail()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockDbSet.When(x => x.Update(null!))
			.Do(_ => throw new ArgumentNullException("entity"));
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

		// Act
		var result = await repository.UpdateAsync(null!);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to update Category");
	}
}
