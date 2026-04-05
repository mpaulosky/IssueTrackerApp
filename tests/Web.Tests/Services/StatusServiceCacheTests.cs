// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     StatusServiceCacheTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests
// =============================================

using Domain.Features.Statuses.Commands;
using Domain.Features.Statuses.Queries;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for StatusService cache-aside behaviour.
///   Uses a real MemoryDistributedCache so cache semantics are accurate.
/// </summary>
public sealed class StatusServiceCacheTests
{
	private readonly IMediator _mediator;
	private readonly StatusService _sut;

	public StatusServiceCacheTests()
	{
		_mediator = Substitute.For<IMediator>();
		var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		var cacheLogger = Substitute.For<ILogger<DistributedCacheHelper>>();
		var cacheHelper = new DistributedCacheHelper(cache, cacheLogger);
		_sut = new StatusService(_mediator, cacheHelper);
	}

	#region GetStatusesAsync cache tests

	[Fact]
	public async Task GetStatusesAsync_FirstCall_HitsMediatorAndCachesResult()
	{
		// Arrange
		var statuses = new List<StatusDto> { CreateTestStatusDto("Open") };
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		// Act
		var result = await _sut.GetStatusesAsync();

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetStatusesAsync_SecondCall_ReturnsCachedResultWithoutHittingMediator()
	{
		// Arrange
		var statuses = new List<StatusDto> { CreateTestStatusDto("Open") };
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		// Act — first call populates cache, second should serve from cache
		await _sut.GetStatusesAsync(includeArchived: false);
		var result = await _sut.GetStatusesAsync(includeArchived: false);

		// Assert — mediator called exactly once
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(1);
		await _mediator.Received(1).Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetStatusesAsync_DifferentIncludeArchivedValues_UsesSeparateCacheKeys()
	{
		// Arrange
		var active = new List<StatusDto> { CreateTestStatusDto("Open") };
		var all = new List<StatusDto> { CreateTestStatusDto("Open"), CreateTestStatusDto("Archived", archived: true) };

		_mediator.Send(Arg.Is<GetStatusesQuery>(q => !q.IncludeArchived), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(active));
		_mediator.Send(Arg.Is<GetStatusesQuery>(q => q.IncludeArchived), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(all));

		// Act
		var r1 = await _sut.GetStatusesAsync(includeArchived: false);
		var r2 = await _sut.GetStatusesAsync(includeArchived: true);

		// Assert — two different cache keys → two mediator calls
		r1.Value!.Count().Should().Be(1);
		r2.Value!.Count().Should().Be(2);
		await _mediator.Received(2).Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetStatusesAsync_WhenMediatorFails_DoesNotCacheAndRetries()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<StatusDto>>("DB error"));

		// Act
		await _sut.GetStatusesAsync();
		var result = await _sut.GetStatusesAsync();

		// Assert — failure not cached; mediator called both times
		result.Success.Should().BeFalse();
		await _mediator.Received(2).Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region GetStatusByIdAsync cache tests

	[Fact]
	public async Task GetStatusByIdAsync_FirstCall_HitsMediatorAndCachesResult()
	{
		// Arrange
		var dto = CreateTestStatusDto("Open");
		_mediator.Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dto));

		// Act
		var result = await _sut.GetStatusByIdAsync("status-1");

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetStatusByIdAsync_SecondCallSameId_ReturnsCachedResultWithoutHittingMediator()
	{
		// Arrange
		var dto = CreateTestStatusDto("Open");
		_mediator.Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dto));

		// Act
		await _sut.GetStatusByIdAsync("status-1");
		var result = await _sut.GetStatusByIdAsync("status-1");

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Write operation invalidation tests

	[Fact]
	public async Task CreateStatusAsync_OnSuccess_EvictsListCache()
	{
		// Arrange — prime both list cache entries
		var existing = new List<StatusDto> { CreateTestStatusDto("Open") };
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(existing));

		await _sut.GetStatusesAsync(includeArchived: false);
		await _sut.GetStatusesAsync(includeArchived: true);

		var created = CreateTestStatusDto("In Progress");
		_mediator.Send(Arg.Any<CreateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(created));
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(new List<StatusDto> { CreateTestStatusDto("Open"), created }));

		// Act
		await _sut.CreateStatusAsync("In Progress", "In Progress Desc");

		// Next read must re-hit mediator (cache was evicted)
		await _sut.GetStatusesAsync(includeArchived: false);

		// Assert — GetStatusesQuery called 3 times: 2 primes + 1 after eviction
		await _mediator.Received(3).Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdateStatusAsync_OnSuccess_EvictsByIdCacheAndListCache()
	{
		// Arrange — prime per-id and list cache
		var dto = CreateTestStatusDto("Open");
		_mediator.Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dto));
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(new List<StatusDto> { dto }));

		await _sut.GetStatusByIdAsync("status-1");
		await _sut.GetStatusesAsync(includeArchived: false);

		var updated = CreateTestStatusDto("Open Updated");
		_mediator.Send(Arg.Any<UpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updated));

		// Act
		await _sut.UpdateStatusAsync("status-1", "Open Updated", "Desc");

		// Subsequent reads should re-hit mediator
		await _sut.GetStatusByIdAsync("status-1");
		await _sut.GetStatusesAsync(includeArchived: false);

		// Assert — each read called once before update, once after
		await _mediator.Received(2).Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>());
		await _mediator.Received(2).Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ArchiveStatusAsync_OnSuccess_EvictsByIdCacheAndListCache()
	{
		// Arrange — prime per-id and list cache
		var dto = CreateTestStatusDto("Open");
		_mediator.Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dto));
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(new List<StatusDto> { dto }));

		await _sut.GetStatusByIdAsync("status-1");
		await _sut.GetStatusesAsync(includeArchived: false);

		var archived = CreateTestStatusDto("Open", archived: true);
		_mediator.Send(Arg.Any<ArchiveStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archived));

		// Act
		await _sut.ArchiveStatusAsync("status-1", archive: true, CreateTestUserDto());

		// Subsequent reads must re-hit mediator
		await _sut.GetStatusByIdAsync("status-1");
		await _sut.GetStatusesAsync(includeArchived: false);

		// Assert — each read twice: once before archive, once after
		await _mediator.Received(2).Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>());
		await _mediator.Received(2).Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CreateStatusAsync_OnFailure_DoesNotEvictCache()
	{
		// Arrange — prime list cache
		var existing = new List<StatusDto> { CreateTestStatusDto("Open") };
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(existing));

		await _sut.GetStatusesAsync(includeArchived: false);

		_mediator.Send(Arg.Any<CreateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<StatusDto>("Validation error"));

		// Act
		await _sut.CreateStatusAsync("Bad", "Bad Desc");

		// Reading again should still hit cache (no eviction on failure)
		await _sut.GetStatusesAsync(includeArchived: false);

		// Assert — list still served from cache, only 1 mediator call total
		await _mediator.Received(1).Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Helper methods

	private static StatusDto CreateTestStatusDto(string name, bool archived = false)
	{
		return new StatusDto(
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
