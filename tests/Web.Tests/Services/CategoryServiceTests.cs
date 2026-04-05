// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Domain.Features.Categories.Commands;
using Domain.Features.Categories.Queries;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for CategoryService facade operations.
///   Tests CRUD orchestration and MediatR integration.
/// </summary>
public sealed class CategoryServiceTests
{
	private readonly IMediator _mediator;
	private readonly CategoryService _sut;

	public CategoryServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		var cacheLogger = Substitute.For<ILogger<DistributedCacheHelper>>();
		var cacheHelper = new DistributedCacheHelper(cache, cacheLogger);
		_sut = new CategoryService(_mediator, cacheHelper);
	}

	#region GetCategoriesAsync Tests

	[Fact]
	public async Task GetCategoriesAsync_WithDefaultParams_ReturnsCategories()
	{
		// Arrange
		var categories = new List<CategoryDto>
		{
			CreateTestCategoryDto("Bug"),
			CreateTestCategoryDto("Feature")
		};
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetCategoriesAsync_WithIncludeArchivedFilter_SendsCorrectQuery()
	{
		// Arrange
		var categories = new List<CategoryDto>();
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		await _sut.GetCategoriesAsync(includeArchived: true, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<GetCategoriesQuery>(q => q.IncludeArchived == true),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_WhenMediatorFails_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<CategoryDto>>("Database error"));

		// Act
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Database error");
	}

	#endregion

	#region GetCategoryByIdAsync Tests

	[Fact]
	public async Task GetCategoryByIdAsync_WithValidId_ReturnsCategory()
	{
		// Arrange
		var categoryDto = CreateTestCategoryDto("Test Category");
		_mediator.Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(categoryDto));

		// Act
		var result = await _sut.GetCategoryByIdAsync("test-id");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.CategoryName.Should().Be("Test Category");
	}

	[Fact]
	public async Task GetCategoryByIdAsync_WithInvalidId_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CategoryDto>("Category not found"));

		// Act
		var result = await _sut.GetCategoryByIdAsync("invalid-id");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	#endregion

	#region CreateCategoryAsync Tests

	[Fact]
	public async Task CreateCategoryAsync_WithValidData_ReturnsCreatedCategory()
	{
		// Arrange
		var createdCategory = CreateTestCategoryDto("New Category");
		_mediator.Send(Arg.Any<CreateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdCategory));

		// Act
		var result = await _sut.CreateCategoryAsync("New Category", "New Description");

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.CategoryName.Should().Be("New Category");
	}

	[Fact]
	public async Task CreateCategoryAsync_SendsCorrectCommand()
	{
		// Arrange
		var createdCategory = CreateTestCategoryDto("Test Category");
		_mediator.Send(Arg.Any<CreateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdCategory));

		// Act
		await _sut.CreateCategoryAsync("Test Category", "Test Description", CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<CreateCategoryCommand>(c =>
				c.CategoryName == "Test Category" &&
				c.CategoryDescription == "Test Description"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CreateCategoryAsync_WhenValidationFails_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<CreateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CategoryDto>("A category with this name already exists"));

		// Act
		var result = await _sut.CreateCategoryAsync("Duplicate", "Description");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("already exists");
	}

	#endregion

	#region UpdateCategoryAsync Tests

	[Fact]
	public async Task UpdateCategoryAsync_WithValidData_ReturnsUpdatedCategory()
	{
		// Arrange
		var updatedCategory = CreateTestCategoryDto("Updated Category");
		_mediator.Send(Arg.Any<UpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedCategory));

		// Act
		var result = await _sut.UpdateCategoryAsync("id", "Updated Category", "Updated Description");

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.CategoryName.Should().Be("Updated Category");
	}

	[Fact]
	public async Task UpdateCategoryAsync_SendsCorrectCommand()
	{
		// Arrange
		var updatedCategory = CreateTestCategoryDto("Test Category");
		_mediator.Send(Arg.Any<UpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedCategory));

		// Act
		await _sut.UpdateCategoryAsync("cat-123", "Test Category", "Test Description", CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<UpdateCategoryCommand>(c =>
				c.Id == "cat-123" &&
				c.CategoryName == "Test Category" &&
				c.CategoryDescription == "Test Description"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdateCategoryAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<UpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CategoryDto>("Category not found"));

		// Act
		var result = await _sut.UpdateCategoryAsync("invalid-id", "Title", "Desc");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	#endregion

	#region ArchiveCategoryAsync Tests

	[Fact]
	public async Task ArchiveCategoryAsync_WhenArchiving_ReturnsArchivedCategory()
	{
		// Arrange
		var archivedCategory = CreateTestCategoryDto("Archived Category", archived: true);
		_mediator.Send(Arg.Any<ArchiveCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archivedCategory));

		// Act
		var result = await _sut.ArchiveCategoryAsync("id", archive: true, CreateTestUserDto());

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Archived.Should().BeTrue();
	}

	[Fact]
	public async Task ArchiveCategoryAsync_WhenUnarchiving_ReturnsUnarchivedCategory()
	{
		// Arrange
		var unarchivedCategory = CreateTestCategoryDto("Unarchived Category", archived: false);
		_mediator.Send(Arg.Any<ArchiveCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(unarchivedCategory));

		// Act
		var result = await _sut.ArchiveCategoryAsync("id", archive: false, CreateTestUserDto());

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Archived.Should().BeFalse();
	}

	[Fact]
	public async Task ArchiveCategoryAsync_SendsCorrectCommand()
	{
		// Arrange
		var archivedCategory = CreateTestCategoryDto("Test Category", archived: true);
		var user = CreateTestUserDto();
		_mediator.Send(Arg.Any<ArchiveCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archivedCategory));

		// Act
		await _sut.ArchiveCategoryAsync("cat-123", archive: true, user, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<ArchiveCategoryCommand>(c =>
				c.Id == "cat-123" &&
				c.Archive == true &&
				c.ArchivedBy.Id == user.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ArchiveCategoryAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<ArchiveCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CategoryDto>("Category not found"));

		// Act
		var result = await _sut.ArchiveCategoryAsync("invalid-id", true, CreateTestUserDto());

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	#endregion

	#region Helper Methods

	private static CategoryDto CreateTestCategoryDto(string name, bool archived = false)
	{
		return new CategoryDto(
			ObjectId.GenerateNewId(),
			name,
			$"{name} Description",
			DateTime.UtcNow,
			null,
			archived,
			archived ? CreateTestUserDto() : UserDto.Empty);
	}

	private static UserDto CreateTestUserDto()
	{
		return new UserDto("user1", "Test User", "test@example.com");
	}

	#endregion
}
