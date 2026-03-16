// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryGetAllTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Domain.Models;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository GetAllAsync method.
/// </summary>
public class RepositoryGetAllTests : RepositoryTestBase<Category>
{

	[Fact]
	public async Task GetAllAsync_WhenExceptionOccurs_Should_ReturnFail()
	{
		// Arrange
		SetupEmptyDbSet();

		// Act
		var result = await Sut.GetAllAsync();

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllAsync_Should_ReturnResult()
	{
		// Arrange
		SetupEmptyDbSet();

		// Act
		var result = await Sut.GetAllAsync();

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}
}
