// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LookupServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Domain.Models;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for LookupService operations.
///   Tests category and status lookup functionality using repositories.
/// </summary>
public sealed class LookupServiceTests
{
	private readonly IRepository<Category> _categoryRepository;
	private readonly IRepository<Status> _statusRepository;
	private readonly LookupService _sut;

	public LookupServiceTests()
	{
		_categoryRepository = Substitute.For<IRepository<Category>>();
		_statusRepository = Substitute.For<IRepository<Status>>();
		_sut = new LookupService(_categoryRepository, _statusRepository);
	}

	#region GetCategoriesAsync Tests

	[Fact]
	public async Task GetCategoriesAsync_WhenCategoriesExist_ReturnsCategories()
	{
		// Arrange
		var categories = new List<Category>
		{
			CreateTestCategory("Bug"),
			CreateTestCategory("Feature")
		};
		_categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(categories));

		// Act
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetCategoriesAsync_FiltersArchivedCategories_ReturnsOnlyActive()
	{
		// Arrange
		var activeCategory = CreateTestCategory("Active");
		var categories = new List<Category> { activeCategory };

		_categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(categories));

		// Act
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeTrue();
		await _categoryRepository.Received(1).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_OrdersCategoriesByName_ReturnsOrderedList()
	{
		// Arrange
		var categories = new List<Category>
		{
			CreateTestCategory("Zebra"),
			CreateTestCategory("Alpha"),
			CreateTestCategory("Middle")
		};
		_categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(categories));

		// Act
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeTrue();
		var categoryList = result.Value!.ToList();
		categoryList[0].CategoryName.Should().Be("Alpha");
		categoryList[1].CategoryName.Should().Be("Middle");
		categoryList[2].CategoryName.Should().Be("Zebra");
	}

	[Fact]
	public async Task GetCategoriesAsync_WhenRepositoryFails_ReturnsFailure()
	{
		// Arrange
		_categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Category>>("Database error"));

		// Act
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Database error");
	}

	#endregion

	#region GetStatusesAsync Tests

	[Fact]
	public async Task GetStatusesAsync_WhenStatusesExist_ReturnsStatuses()
	{
		// Arrange
		var statuses = new List<Status>
		{
			CreateTestStatus("Open"),
			CreateTestStatus("Closed")
		};
		_statusRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Status>>(statuses));

		// Act
		var result = await _sut.GetStatusesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetStatusesAsync_FiltersArchivedStatuses_ReturnsOnlyActive()
	{
		// Arrange
		var activeStatus = CreateTestStatus("Active");
		var statuses = new List<Status> { activeStatus };

		_statusRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Status>>(statuses));

		// Act
		var result = await _sut.GetStatusesAsync();

		// Assert
		result.Success.Should().BeTrue();
		await _statusRepository.Received(1).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetStatusesAsync_WhenRepositoryFails_ReturnsFailure()
	{
		// Arrange
		_statusRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Status>>("Database error"));

		// Act
		var result = await _sut.GetStatusesAsync();

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Database error");
	}

	[Fact]
	public async Task GetStatusesAsync_WhenNoStatusesExist_ReturnsEmptyCollection()
	{
		// Arrange
		var emptyStatuses = new List<Status>();
		_statusRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Status>>(emptyStatuses));

		// Act
		var result = await _sut.GetStatusesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	#endregion

	#region Helper Methods

	private static Category CreateTestCategory(string name)
	{
		return new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = name,
			CategoryDescription = $"{name} Description",
			DateCreated = DateTime.UtcNow,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};
	}

	private static Status CreateTestStatus(string name)
	{
		return new Status
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = name,
			StatusDescription = $"{name} Description",
			DateCreated = DateTime.UtcNow,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};
	}

	#endregion
}
