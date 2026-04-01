// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AnalyticsServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Text.Json;

using Domain.Abstractions;
using Domain.DTOs.Analytics;
using Domain.Features.Analytics.Queries;

using MediatR;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for AnalyticsService caching behavior.
/// </summary>
public sealed class AnalyticsServiceTests
{
	private readonly IMediator _mediator;
	private readonly IDistributedCache _cache;
	private readonly ILogger<AnalyticsService> _logger;
	private readonly AnalyticsService _sut;

	public AnalyticsServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		_cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		_logger = Substitute.For<ILogger<AnalyticsService>>();
		_sut = new AnalyticsService(_mediator, _cache, _logger);
	}

	private static async Task SetCacheAsync<T>(IDistributedCache cache, string key, T value)
	{
		var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
		await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
		});
	}

	#region Cache Hit Tests

	[Fact]
	public async Task GetAnalyticsSummaryAsync_CacheHit_ReturnsCachedDataWithoutRepositoryCall()
	{
		// Arrange
		var startDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 1, 31, 0, 0, 0, DateTimeKind.Utc);
		var cacheKey = $"analytics_summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		var cachedSummary = CreateTestAnalyticsSummary();
		await SetCacheAsync(_cache, cacheKey, cachedSummary);

		// Act
		var result = await _sut.GetAnalyticsSummaryAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(cachedSummary);
		await _mediator.DidNotReceive().Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssuesByStatusAsync_CacheHit_ReturnsCachedDataWithoutRepositoryCall()
	{
		// Arrange
		var startDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 2, 28, 0, 0, 0, DateTimeKind.Utc);
		var cacheKey = $"analytics_status_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		var cachedData = CreateTestIssuesByStatus();
		await SetCacheAsync(_cache, cacheKey, cachedData);

		// Act
		var result = await _sut.GetIssuesByStatusAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(cachedData);
		await _mediator.DidNotReceive().Send(Arg.Any<GetIssuesByStatusQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssuesByCategoryAsync_CacheHit_ReturnsCachedDataWithoutRepositoryCall()
	{
		// Arrange
		var startDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Utc);
		var cacheKey = $"analytics_category_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		var cachedData = CreateTestIssuesByCategory();
		await SetCacheAsync(_cache, cacheKey, cachedData);

		// Act
		var result = await _sut.GetIssuesByCategoryAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(cachedData);
		await _mediator.DidNotReceive().Send(Arg.Any<GetIssuesByCategoryQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Cache Miss Tests

	[Fact]
	public async Task GetAnalyticsSummaryAsync_CacheMiss_CallsMediatorAndCachesResult()
	{
		// Arrange
		var startDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 4, 30, 0, 0, 0, DateTimeKind.Utc);

		var freshSummary = CreateTestAnalyticsSummary();

		_mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(freshSummary));

		// Act
		var result = await _sut.GetAnalyticsSummaryAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(freshSummary);
		await _mediator.Received(1).Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetAnalyticsSummaryAsync_SecondCall_HitsCache()
	{
		// Arrange
		var startDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 4, 30, 0, 0, 0, DateTimeKind.Utc);
		var summary = CreateTestAnalyticsSummary();

		_mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(summary));

		// Act — prime cache on first call, hit cache on second
		await _sut.GetAnalyticsSummaryAsync(startDate, endDate);
		var result = await _sut.GetAnalyticsSummaryAsync(startDate, endDate);

		// Assert — mediator called only once
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssuesByStatusAsync_CacheMiss_CallsMediatorAndCachesResult()
	{
		// Arrange
		var startDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc);
		var freshData = CreateTestIssuesByStatus();

		_mediator.Send(Arg.Any<GetIssuesByStatusQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<IssuesByStatusDto>>(freshData));

		// Act
		var result = await _sut.GetIssuesByStatusAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(freshData);
		await _mediator.Received(1).Send(Arg.Any<GetIssuesByStatusQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssuesOverTimeAsync_CacheMiss_CallsMediatorAndCachesResult()
	{
		// Arrange
		var startDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
		var freshData = CreateTestIssuesOverTime();

		_mediator.Send(Arg.Any<GetIssuesOverTimeQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<IssuesOverTimeDto>>(freshData));

		// Act
		var result = await _sut.GetIssuesOverTimeAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(freshData);
		await _mediator.Received(1).Send(Arg.Any<GetIssuesOverTimeQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetResolutionTimesAsync_CacheMiss_CallsMediatorAndCachesResult()
	{
		// Arrange
		var startDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc);
		var freshData = CreateTestResolutionTimes();

		_mediator.Send(Arg.Any<GetResolutionTimesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<ResolutionTimeDto>>(freshData));

		// Act
		var result = await _sut.GetResolutionTimesAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(freshData);
		await _mediator.Received(1).Send(Arg.Any<GetResolutionTimesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetTopContributorsAsync_CacheMiss_CallsMediatorAndCachesResult()
	{
		// Arrange
		var startDate = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 8, 31, 0, 0, 0, DateTimeKind.Utc);
		var topCount = 5;
		var freshData = CreateTestTopContributors();

		_mediator.Send(Arg.Any<GetTopContributorsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<TopContributorDto>>(freshData));

		// Act
		var result = await _sut.GetTopContributorsAsync(startDate, endDate, topCount);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(freshData);
		await _mediator.Received(1).Send(Arg.Any<GetTopContributorsQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetAnalyticsSummaryAsync_FailedResult_DoesNotCacheResult()
	{
		// Arrange
		var startDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 9, 30, 0, 0, 0, DateTimeKind.Utc);

		_mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<AnalyticsSummaryDto>("Database error"));

		// Act
		var result = await _sut.GetAnalyticsSummaryAsync(startDate, endDate);

		// Assert — failed result returned; second call still hits mediator (nothing was cached)
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Database error");

		await _sut.GetAnalyticsSummaryAsync(startDate, endDate);
		await _mediator.Received(2).Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Different Cache Keys Tests

	[Fact]
	public async Task GetAnalyticsSummaryAsync_DifferentDateRanges_UsesDifferentCacheKeys()
	{
		// Arrange
		var startDate1 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate1 = new DateTime(2025, 1, 31, 0, 0, 0, DateTimeKind.Utc);
		var startDate2 = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate2 = new DateTime(2025, 2, 28, 0, 0, 0, DateTimeKind.Utc);

		var summary1 = CreateTestAnalyticsSummary(totalIssues: 100);
		var summary2 = CreateTestAnalyticsSummary(totalIssues: 200);

		_mediator.Send(Arg.Is<GetAnalyticsSummaryQuery>(q => q.StartDate == startDate1), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(summary1));
		_mediator.Send(Arg.Is<GetAnalyticsSummaryQuery>(q => q.StartDate == startDate2), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(summary2));

		// Act
		var result1 = await _sut.GetAnalyticsSummaryAsync(startDate1, endDate1);
		var result2 = await _sut.GetAnalyticsSummaryAsync(startDate2, endDate2);

		// Assert
		result1.Value!.TotalIssues.Should().Be(100);
		result2.Value!.TotalIssues.Should().Be(200);
		await _mediator.Received(2).Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetTopContributorsAsync_DifferentTopCounts_UsesDifferentCacheKeys()
	{
		// Arrange
		var startDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 1, 31, 0, 0, 0, DateTimeKind.Utc);

		var top5Data = CreateTestTopContributors(count: 5);
		var top10Data = CreateTestTopContributors(count: 10);

		_mediator.Send(Arg.Is<GetTopContributorsQuery>(q => q.TopCount == 5), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<TopContributorDto>>(top5Data));
		_mediator.Send(Arg.Is<GetTopContributorsQuery>(q => q.TopCount == 10), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<TopContributorDto>>(top10Data));

		// Act
		var result5 = await _sut.GetTopContributorsAsync(startDate, endDate, topCount: 5);
		var result10 = await _sut.GetTopContributorsAsync(startDate, endDate, topCount: 10);

		// Assert
		result5.Value!.Count.Should().Be(5);
		result10.Value!.Count.Should().Be(10);
		await _mediator.Received(2).Send(Arg.Any<GetTopContributorsQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetAnalyticsSummaryAsync_NullDates_GeneratesConsistentCacheKey()
	{
		// Arrange
		var summary = CreateTestAnalyticsSummary();

		_mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(summary));

		// Act — call twice with null dates
		var result1 = await _sut.GetAnalyticsSummaryAsync(null, null);
		var result2 = await _sut.GetAnalyticsSummaryAsync(null, null);

		// Assert — second call hits cache
		result1.Value.Should().BeEquivalentTo(summary);
		result2.Value.Should().BeEquivalentTo(summary);
		await _mediator.Received(1).Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Cache Expiration Tests

	[Fact]
	public async Task GetAnalyticsSummaryAsync_CacheExpired_TriggersFreshFetch()
	{
		// Arrange
		var startDate = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 10, 31, 0, 0, 0, DateTimeKind.Utc);
		var cacheKey = $"analytics_summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		// Pre-populate with a 1ms TTL so it expires immediately
		var oldSummary = CreateTestAnalyticsSummary(totalIssues: 50);
		var bytes = JsonSerializer.SerializeToUtf8Bytes(oldSummary);
		await _cache.SetAsync(cacheKey, bytes, new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1)
		});

		var newSummary = CreateTestAnalyticsSummary(totalIssues: 75);
		_mediator.Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(newSummary));

		// Wait for cache to expire
		await Task.Delay(50);

		// Act
		var result = await _sut.GetAnalyticsSummaryAsync(startDate, endDate);

		// Assert — stale entry gone; fresh data returned
		result.Value!.TotalIssues.Should().Be(75);
		await _mediator.Received(1).Send(Arg.Any<GetAnalyticsSummaryQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Export (No Caching) Tests

	[Fact]
	public async Task ExportAnalyticsAsync_NeverCachesResults()
	{
		// Arrange
		var startDate = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2025, 11, 30, 0, 0, 0, DateTimeKind.Utc);
		var exportData = new byte[] { 0x01, 0x02, 0x03 };

		_mediator.Send(Arg.Any<ExportAnalyticsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(exportData));

		// Act — call twice; exports are never cached
		var result1 = await _sut.ExportAnalyticsAsync(startDate, endDate);
		var result2 = await _sut.ExportAnalyticsAsync(startDate, endDate);

		// Assert
		result1.Value.Should().BeEquivalentTo(exportData);
		result2.Value.Should().BeEquivalentTo(exportData);
		await _mediator.Received(2).Send(Arg.Any<ExportAnalyticsQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Helper Methods

	private static AnalyticsSummaryDto CreateTestAnalyticsSummary(int totalIssues = 100)
	{
		var byStatus = new List<IssuesByStatusDto>
		{
			new("Open", totalIssues / 2),
			new("Closed", totalIssues / 2)
		};

		var byCategory = new List<IssuesByCategoryDto>
		{
			new("Bug", 60),
			new("Feature", 40)
		};

		var overTime = new List<IssuesOverTimeDto>
		{
			new(DateTime.UtcNow.Date, 5, 3)
		};

		var topContributors = new List<TopContributorDto>
		{
			new("user1", "User One", 10, 25)
		};

		return new AnalyticsSummaryDto(
			TotalIssues: totalIssues,
			OpenIssues: totalIssues / 2,
			ClosedIssues: totalIssues / 2,
			AverageResolutionHours: 24.5,
			ByStatus: byStatus,
			ByCategory: byCategory,
			OverTime: overTime,
			TopContributors: topContributors);
	}

	private static List<IssuesByStatusDto> CreateTestIssuesByStatus()
	{
		return
		[
			new("Open", 10),
			new("In Progress", 5),
			new("Closed", 15)
		];
	}

	private static List<IssuesByCategoryDto> CreateTestIssuesByCategory()
	{
		return
		[
			new("Bug", 20),
			new("Feature", 10),
			new("Enhancement", 5)
		];
	}

	private static List<IssuesOverTimeDto> CreateTestIssuesOverTime()
	{
		return
		[
			new(DateTime.UtcNow.Date.AddDays(-2), 3, 1),
			new(DateTime.UtcNow.Date.AddDays(-1), 5, 2),
			new(DateTime.UtcNow.Date, 4, 3)
		];
	}

	private static List<ResolutionTimeDto> CreateTestResolutionTimes()
	{
		return
		[
			new("Bug", 24.5),
			new("Feature", 48.0),
			new("Enhancement", 12.0)
		];
	}

	private static List<TopContributorDto> CreateTestTopContributors(int count = 3)
	{
		return Enumerable.Range(1, count)
			.Select(i => new TopContributorDto($"user{i}", $"User {i}", 10 - i, 20 - i))
			.ToList();
	}

	#endregion
}
