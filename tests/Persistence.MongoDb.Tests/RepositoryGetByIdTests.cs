// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryGetByIdTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Domain.Abstractions;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.MongoDb.Configurations;
using Persistence.MongoDb.Repositories;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository GetByIdAsync method.
/// </summary>
public class RepositoryGetByIdTests
{
	/// <summary>
	///   Helper method to create a test IssueTrackerDbContext.
	/// </summary>
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

	/// <summary>
	///   Helper method to create a test logger.
	/// </summary>
	private static ILogger<Repository<Category>> CreateTestLogger()
	{
		return Substitute.For<ILogger<Repository<Category>>>();
	}

	[Fact]
	public async Task GetByIdAsync_WithInvalidObjectId_Should_ReturnFailWithValidationError()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		const string invalidId = "not-a-valid-objectid";

		// Act
		var result = await repository.GetByIdAsync(invalidId);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Contain("Invalid ID format");
		result.Error.Should().Contain(invalidId);
	}

	[Fact]
	public async Task GetByIdAsync_WithEmptyString_Should_ReturnFailWithValidationError()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		var emptyId = string.Empty;

		// Act
		var result = await repository.GetByIdAsync(emptyId);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Contain("Invalid ID format");
	}

	[Fact]
	public async Task GetByIdAsync_WithNullId_Should_ReturnFailWithValidationError()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		string? nullId = null;

		// Act
		var result = await repository.GetByIdAsync(nullId!);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Contain("Invalid ID format");
	}

	[Fact]
	public async Task GetByIdAsync_WithWhitespace_Should_ReturnFailWithValidationError()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		const string whitespaceId = "   ";

		// Act
		var result = await repository.GetByIdAsync(whitespaceId);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Contain("Invalid ID format");
	}

	[Fact]
	public async Task GetByIdAsync_WithValidObjectId_WhenEntityNotFound_Should_ReturnFailWithNotFoundError()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		var validObjectId = ObjectId.GenerateNewId().ToString();

		// Act
		// This will throw an exception because MongoDB is not running, which is caught by the catch block
		var result = await repository.GetByIdAsync(validObjectId);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		// The exception is caught and returns a generic failure
		result.Error.Should().Contain("Failed to retrieve Category");
	}

	[Fact]
	public async Task GetByIdAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();
		var repository = new Repository<Category>(context, logger);
		var validObjectId = ObjectId.GenerateNewId().ToString();

		// Act
		// This will throw an exception because MongoDB is not running
		var result = await repository.GetByIdAsync(validObjectId);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.Error.Should().NotBeNullOrEmpty();
		result.Error.Should().Contain("Failed to retrieve");
	}
}
