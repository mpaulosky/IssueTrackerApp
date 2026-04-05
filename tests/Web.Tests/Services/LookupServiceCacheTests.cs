// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LookupServiceCacheTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests
// =============================================

using Domain.Models;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for LookupService cache-aside behaviour.
///   Uses a real MemoryDistributedCache so cache semantics are accurate.
/// </summary>
public sealed class LookupServiceCacheTests
{
	private readonly IRepository<Category> _categoryRepository;
	private readonly IRepository<Status> _statusRepository;
	private readonly LookupService _sut;

	public LookupServiceCacheTests()
	{
		_categoryRepository = Substitute.For<IRepository<Category>>();
		_statusRepository = Substitute.For<IRepository<Status>>();
		var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		var cacheLogger = Substitute.For<ILogger<DistributedCacheHelper>>();
		var cacheHelper = new DistributedCacheHelper(cache, cacheLogger);
		_sut = new LookupService(_categoryRepository, _statusRepository, cacheHelper);
	}

	#region GetCategoriesAsync cache tests

	[Fact]
	public async Task GetCategoriesAsync_FirstCall_HitsRepositoryAndCachesResult()
	{
		// Arrange
		var categories = new List<Category> { CreateTestCategory("Bug") };
		_categoryRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(categories));

		// Act
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(1);
		await _categoryRepository.Received(1).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_SecondCall_ReturnsCachedResultWithoutHittingRepository()
	{
		// Arrange
		var categories = new List<Category> { CreateTestCategory("Bug"), CreateTestCategory("Feature") };
		_categoryRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(categories));

		// Act — first call populates cache, second should serve from cache
		await _sut.GetCategoriesAsync();
		var result = await _sut.GetCategoriesAsync();

		// Assert — repository called exactly once
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		await _categoryRepository.Received(1).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_WhenRepositoryFails_DoesNotCacheAndRetries()
	{
		// Arrange
		_categoryRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Category>>("DB error"));

		// Act
		await _sut.GetCategoriesAsync();
		var result = await _sut.GetCategoriesAsync();

		// Assert — failure not cached; repository called both times
		result.Success.Should().BeFalse();
		await _categoryRepository.Received(2).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_ResultsReturnedOrderedByName()
	{
		// Arrange — unordered source
		var categories = new List<Category>
		{
			CreateTestCategory("Zebra"),
			CreateTestCategory("Alpha"),
			CreateTestCategory("Middle")
		};
		_categoryRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(categories));

		// Act — second call serves from cache; ordering must be preserved
		await _sut.GetCategoriesAsync();
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeTrue();
		var names = result.Value!.Select(c => c.CategoryName).ToList();
		names.Should().BeInAscendingOrder();
	}

	#endregion

	#region GetStatusesAsync cache tests

	[Fact]
	public async Task GetStatusesAsync_FirstCall_HitsRepositoryAndCachesResult()
	{
		// Arrange
		var statuses = new List<Status> { CreateTestStatus("Open") };
		_statusRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Status>>(statuses));

		// Act
		var result = await _sut.GetStatusesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(1);
		await _statusRepository.Received(1).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetStatusesAsync_SecondCall_ReturnsCachedResultWithoutHittingRepository()
	{
		// Arrange
		var statuses = new List<Status> { CreateTestStatus("Open"), CreateTestStatus("Closed") };
		_statusRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Status>>(statuses));

		// Act — first call populates cache, second should serve from cache
		await _sut.GetStatusesAsync();
		var result = await _sut.GetStatusesAsync();

		// Assert — repository called exactly once
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		await _statusRepository.Received(1).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetStatusesAsync_WhenRepositoryFails_DoesNotCacheAndRetries()
	{
		// Arrange
		_statusRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Status>>("DB error"));

		// Act
		await _sut.GetStatusesAsync();
		var result = await _sut.GetStatusesAsync();

		// Assert — failure not cached; repository called both times
		result.Success.Should().BeFalse();
		await _statusRepository.Received(2).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_AndGetStatusesAsync_UseSeparateCacheKeys()
	{
		// Arrange
		var categories = new List<Category> { CreateTestCategory("Bug") };
		var statuses = new List<Status> { CreateTestStatus("Open") };

		_categoryRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(categories));
		_statusRepository.FindAsync(
				Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Status>>(statuses));

		// Act — prime both caches, then call again
		await _sut.GetCategoriesAsync();
		await _sut.GetStatusesAsync();
		await _sut.GetCategoriesAsync();
		await _sut.GetStatusesAsync();

		// Assert — each repository hit exactly once despite two calls each
		await _categoryRepository.Received(1).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(),
			Arg.Any<CancellationToken>());
		await _statusRepository.Received(1).FindAsync(
			Arg.Any<System.Linq.Expressions.Expression<Func<Status, bool>>>(),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region Helper methods

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
