// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryServiceCacheTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests
// =============================================

using Domain.Features.Categories.Commands;
using Domain.Features.Categories.Queries;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for CategoryService cache-aside behaviour.
///   Uses a real MemoryDistributedCache so cache semantics are accurate.
/// </summary>
public sealed class CategoryServiceCacheTests
{
	private readonly IMediator _mediator;
	private readonly CategoryService _sut;

	public CategoryServiceCacheTests()
	{
		_mediator = Substitute.For<IMediator>();
		var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		var cacheLogger = Substitute.For<ILogger<DistributedCacheHelper>>();
		var cacheHelper = new DistributedCacheHelper(cache, cacheLogger);
		_sut = new CategoryService(_mediator, cacheHelper);
	}

	#region GetCategoriesAsync cache tests

	[Fact]
	public async Task GetCategoriesAsync_FirstCall_HitsMediatorAndCachesResult()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategoryDto("Bug") };
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var result = await _sut.GetCategoriesAsync();

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_SecondCall_ReturnsCachedResultWithoutHittingMediator()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategoryDto("Bug") };
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act — first call populates cache, second should serve from cache
		await _sut.GetCategoriesAsync(includeArchived: false);
		var result = await _sut.GetCategoriesAsync(includeArchived: false);

		// Assert — mediator called exactly once
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(1);
		await _mediator.Received(1).Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_DifferentIncludeArchivedValues_UsesSeparateCacheKeys()
	{
		// Arrange
		var active = new List<CategoryDto> { CreateTestCategoryDto("Active") };
		var all = new List<CategoryDto> { CreateTestCategoryDto("Active"), CreateTestCategoryDto("Archived", archived: true) };

		_mediator.Send(Arg.Is<GetCategoriesQuery>(q => !q.IncludeArchived), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(active));
		_mediator.Send(Arg.Is<GetCategoriesQuery>(q => q.IncludeArchived), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(all));

		// Act
		var r1 = await _sut.GetCategoriesAsync(includeArchived: false);
		var r2 = await _sut.GetCategoriesAsync(includeArchived: true);

		// Assert — two different cache keys → two mediator calls
		r1.Value!.Count().Should().Be(1);
		r2.Value!.Count().Should().Be(2);
		await _mediator.Received(2).Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoriesAsync_WhenMediatorFails_DoesNotCacheAndRetries()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<CategoryDto>>("DB error"));

		// Act
		await _sut.GetCategoriesAsync();
		var result = await _sut.GetCategoriesAsync();

		// Assert — failure not cached; mediator called both times
		result.Success.Should().BeFalse();
		await _mediator.Received(2).Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region GetCategoryByIdAsync cache tests

	[Fact]
	public async Task GetCategoryByIdAsync_FirstCall_HitsMediatorAndCachesResult()
	{
		// Arrange
		var dto = CreateTestCategoryDto("Bug");
		_mediator.Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dto));

		// Act
		var result = await _sut.GetCategoryByIdAsync("cat-1");

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetCategoryByIdAsync_SecondCallSameId_ReturnsCachedResultWithoutHittingMediator()
	{
		// Arrange
		var dto = CreateTestCategoryDto("Bug");
		_mediator.Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dto));

		// Act
		await _sut.GetCategoryByIdAsync("cat-1");
		var result = await _sut.GetCategoryByIdAsync("cat-1");

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Write operation invalidation tests

	[Fact]
	public async Task CreateCategoryAsync_OnSuccess_EvictsListCache()
	{
		// Arrange — prime both list cache entries
		var existing = new List<CategoryDto> { CreateTestCategoryDto("Bug") };
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(existing));

		await _sut.GetCategoriesAsync(includeArchived: false);
		await _sut.GetCategoriesAsync(includeArchived: true);

		var created = CreateTestCategoryDto("Feature");
		_mediator.Send(Arg.Any<CreateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(created));
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(new List<CategoryDto> { CreateTestCategoryDto("Bug"), created }));

		// Act
		await _sut.CreateCategoryAsync("Feature", "Feature Desc");

		// Next read must re-hit mediator (cache was evicted)
		await _sut.GetCategoriesAsync(includeArchived: false);

		// Assert — GetCategoriesQuery called 3 times: 2 primes + 1 after eviction
		await _mediator.Received(3).Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdateCategoryAsync_OnSuccess_EvictsByIdCacheAndListCache()
	{
		// Arrange — prime per-id and list cache
		var dto = CreateTestCategoryDto("Bug");
		_mediator.Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dto));
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(new List<CategoryDto> { dto }));

		await _sut.GetCategoryByIdAsync("cat-1");
		await _sut.GetCategoriesAsync(includeArchived: false);

		var updated = CreateTestCategoryDto("Bug Updated");
		_mediator.Send(Arg.Any<UpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updated));

		// Act
		await _sut.UpdateCategoryAsync("cat-1", "Bug Updated", "Desc");

		// Subsequent reads should re-hit mediator
		await _sut.GetCategoryByIdAsync("cat-1");
		await _sut.GetCategoriesAsync(includeArchived: false);

		// Assert — each read called once before update, once after
		await _mediator.Received(2).Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>());
		await _mediator.Received(2).Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ArchiveCategoryAsync_OnSuccess_EvictsByIdCacheAndListCache()
	{
		// Arrange — prime per-id and list cache
		var dto = CreateTestCategoryDto("Bug");
		_mediator.Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dto));
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(new List<CategoryDto> { dto }));

		await _sut.GetCategoryByIdAsync("cat-1");
		await _sut.GetCategoriesAsync(includeArchived: false);

		var archived = CreateTestCategoryDto("Bug", archived: true);
		_mediator.Send(Arg.Any<ArchiveCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archived));

		// Act
		await _sut.ArchiveCategoryAsync("cat-1", archive: true, CreateTestUserDto());

		// Subsequent reads must re-hit mediator
		await _sut.GetCategoryByIdAsync("cat-1");
		await _sut.GetCategoriesAsync(includeArchived: false);

		// Assert — each read twice: once before archive, once after
		await _mediator.Received(2).Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>());
		await _mediator.Received(2).Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CreateCategoryAsync_OnFailure_DoesNotEvictCache()
	{
		// Arrange — prime list cache
		var existing = new List<CategoryDto> { CreateTestCategoryDto("Bug") };
		_mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(existing));

		await _sut.GetCategoriesAsync(includeArchived: false);

		_mediator.Send(Arg.Any<CreateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<CategoryDto>("Validation error"));

		// Act
		await _sut.CreateCategoryAsync("Bad", "Bad Desc");

		// Reading again should still hit cache (no eviction on failure)
		await _sut.GetCategoriesAsync(includeArchived: false);

		// Assert — list still served from cache, only 1 mediator call total
		await _mediator.Received(1).Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Helper methods

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

	private static UserDto CreateTestUserDto() =>
		new("user1", "Test User", "test@example.com");

	#endregion
}
