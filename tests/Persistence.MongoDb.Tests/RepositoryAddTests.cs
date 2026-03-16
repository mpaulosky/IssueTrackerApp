// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryAddTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Persistence.MongoDb.Tests.Helpers;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository AddAsync method.
/// </summary>
public class RepositoryAddTests
{
	[Fact]
	public async Task AddAsync_WhenExceptionOccurs_Should_ReturnFail()
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
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		// Act
		var result = await repository.AddAsync(category);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to add Category");
	}

	[Fact]
	public async Task AddAsync_Should_ReturnResultType()
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
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		// Act
		var result = await repository.AddAsync(category);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeOfType<Result<Category>>();
	}

	[Fact]
	public async Task AddAsync_WithNullEntity_Should_ReturnFail()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = MockDbSetHelper.CreateMockDbSet<Category>(new List<Category>());
		mockDbSet.When(x => x.AddAsync(null!, Arg.Any<CancellationToken>()))
			.Do(_ => throw new ArgumentNullException("entity"));
		mockContext.Set<Category>().Returns(mockDbSet);
		mockContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
		var logger = Substitute.For<ILogger<Repository<Category>>>();
		var repository = new Repository<Category>(mockContext, logger);

		// Act
		var result = await repository.AddAsync(null!);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to add Category");
	}

	[Fact]
	public async Task AddAsync_WithValidEntity_Should_ReturnSuccess()
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
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};

		// Act
		var result = await repository.AddAsync(category);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().Be(category);
	}
}
