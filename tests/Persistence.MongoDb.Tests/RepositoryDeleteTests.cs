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
public class RepositoryDeleteTests : RepositoryTestBase<Category>
{
	[Fact]
	public async Task DeleteAsync_WithInvalidObjectId_Should_ReturnFailWithValidationError()
	{
		// Arrange
		SetupEmptyDbSet();
		var invalidId = "invalid-object-id";

		// Act
		var result = await Sut.DeleteAsync(invalidId);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Invalid ID format");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		VerifySaveChangesNotCalled();
	}

	[Fact]
	public async Task DeleteAsync_WithEmptyString_Should_ReturnFailWithValidationError()
	{
		// Arrange
		SetupEmptyDbSet();
		var emptyId = string.Empty;

		// Act
		var result = await Sut.DeleteAsync(emptyId);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Invalid ID format");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		VerifySaveChangesNotCalled();
	}

	[Fact]
	public async Task DeleteAsync_WithNullId_Should_ReturnFailWithValidationError()
	{
		// Arrange
		SetupEmptyDbSet();

		// Act
		var result = await Sut.DeleteAsync(null!);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Invalid ID format");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		VerifySaveChangesNotCalled();
	}

	[Fact]
	public async Task DeleteAsync_WithWhitespace_Should_ReturnFailWithValidationError()
	{
		// Arrange
		SetupEmptyDbSet();
		var whitespaceId = "   ";

		// Act
		var result = await Sut.DeleteAsync(whitespaceId);

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Invalid ID format");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		VerifySaveChangesNotCalled();
	}

	[Fact]
	public async Task DeleteAsync_WithValidObjectId_WhenEntityNotFound_Should_ReturnFail()
	{
		// Arrange
		var validId = ObjectId.GenerateNewId();
		SetupDbSetWithFind(new List<Category>(), e => e.Id);

		// Act
		var result = await Sut.DeleteAsync(validId.ToString());

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
		VerifySaveChangesNotCalled();
	}

	[Fact]
	public async Task DeleteAsync_WithValidObjectId_WhenEntityExists_Should_ReturnSuccess()
	{
		// Arrange
		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};
		SetupDbSetWithFind(new List<Category> { category }, e => e.Id);
		SetupSaveChangesAsync();

		// Act
		var result = await Sut.DeleteAsync(category.Id.ToString());

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Failure.Should().BeFalse();
		VerifySaveChangesCalledOnce();
	}

	[Fact]
	public async Task DeleteAsync_WhenSaveChangesFails_Should_ReturnFail()
	{
		// Arrange
		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Test Category",
			CategoryDescription = "Test Description"
		};
		SetupDbSetWithFind(new List<Category> { category }, e => e.Id);
		SetupSaveChangesToThrow(new Exception("Database error"));

		// Act
		var result = await Sut.DeleteAsync(category.Id.ToString());

		// Assert
		result.Should().NotBeNull();
		result.Failure.Should().BeTrue();
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Database error");
		VerifySaveChangesCalledOnce();
		VerifyErrorLogged();
	}
}
