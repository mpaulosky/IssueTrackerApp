// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryDeleteTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository DeleteAsync method.
/// </summary>
public class RepositoryDeleteTests
{
	private static IssueTrackerDbContext CreateTestContext()
	{
		var options = new DbContextOptionsBuilder<IssueTrackerDbContext>()
			.UseMongoDB("mongodb://localhost:27017", "test-db")
			.Options;

		var settings = Options.Create(new MongoDbSettings
		{
			ConnectionString = "mongodb://localhost:27017",
			DatabaseName = "test-db"
		});

		return new IssueTrackerDbContext(options, settings);
	}

	[Fact]
	public async Task DeleteAsync_WithInvalidObjectId_Should_ReturnFailWithValidationError()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var invalidId = "invalid-object-id";

		// Act
		var result = await repository.DeleteAsync(invalidId);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Invalid ID format");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task DeleteAsync_WithEmptyString_Should_ReturnFailWithValidationError()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var emptyId = string.Empty;

		// Act
		var result = await repository.DeleteAsync(emptyId);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Invalid ID format");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task DeleteAsync_WithNullId_Should_ReturnFailWithValidationError()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		// Act
		var result = await repository.DeleteAsync(null!);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Invalid ID format");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task DeleteAsync_WithWhitespace_Should_ReturnFailWithValidationError()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var whitespaceId = "   ";

		// Act
		var result = await repository.DeleteAsync(whitespaceId);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Invalid ID format");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task DeleteAsync_WithValidObjectId_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		var context = CreateTestContext();
		var logger = NullLogger<Repository<Category>>.Instance;
		var repository = new Repository<Category>(context, logger);

		var validId = ObjectId.GenerateNewId().ToString();

		// Act
		var result = await repository.DeleteAsync(validId);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}
}
