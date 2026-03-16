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

using Persistence.MongoDb.Tests.Helpers;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository GetByIdAsync method.
/// </summary>
public class RepositoryGetByIdTests : RepositoryTestBase<Category>
{

	[Fact]
	public async Task GetByIdAsync_WithInvalidObjectId_Should_ReturnFailWithValidationError()
	{
		// Arrange
		SetupEmptyDbSet();
		const string invalidId = "not-a-valid-objectid";

		// Act
		var result = await Sut.GetByIdAsync(invalidId);

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
		SetupEmptyDbSet();
		var emptyId = string.Empty;

		// Act
		var result = await Sut.GetByIdAsync(emptyId);

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
		SetupEmptyDbSet();
		string? nullId = null;

		// Act
		var result = await Sut.GetByIdAsync(nullId!);

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
		SetupEmptyDbSet();
		const string whitespaceId = "   ";

		// Act
		var result = await Sut.GetByIdAsync(whitespaceId);

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
		var validObjectId = ObjectId.GenerateNewId();
		SetupDbSetWithFind(Enumerable.Empty<Category>(), c => c.Id);

		// Act
		var result = await Sut.GetByIdAsync(validObjectId.ToString());

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("was not found");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task GetByIdAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		var validObjectId = ObjectId.GenerateNewId();
		SetupDbSetWithFind(Enumerable.Empty<Category>(), c => c.Id);

		// Act
		var result = await Sut.GetByIdAsync(validObjectId.ToString());

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.Error.Should().NotBeNullOrEmpty();
		result.Error.Should().Contain("was not found");
	}
}
