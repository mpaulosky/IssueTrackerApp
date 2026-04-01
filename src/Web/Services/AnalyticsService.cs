// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AnalyticsService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using System.Text.Json;

using Domain.Abstractions;
using Domain.DTOs.Analytics;
using Domain.Features.Analytics.Queries;

using MediatR;

using Microsoft.Extensions.Caching.Distributed;

namespace Web.Services;

/// <summary>
/// Service implementation for analytics operations.
/// Uses IDistributedCache (Redis in production, in-memory in test) with a 5-minute TTL.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
	private readonly IMediator _mediator;
	private readonly IDistributedCache _cache;
	private readonly ILogger<AnalyticsService> _logger;
	private const int CacheExpirationMinutes = 5;

	private static readonly DistributedCacheEntryOptions DefaultCacheOptions = new()
	{
		AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
	};

	public AnalyticsService(
		IMediator mediator,
		IDistributedCache cache,
		ILogger<AnalyticsService> logger)
	{
		_mediator = mediator;
		_cache = cache;
		_logger = logger;
	}

	private async Task<T?> GetFromCacheAsync<T>(string cacheKey, CancellationToken cancellationToken)
	{
		var bytes = await _cache.GetAsync(cacheKey, cancellationToken);
		if (bytes is null) return default;
		return JsonSerializer.Deserialize<T>(bytes);
	}

	private async Task SetInCacheAsync<T>(string cacheKey, T value, CancellationToken cancellationToken)
	{
		var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
		await _cache.SetAsync(cacheKey, bytes, DefaultCacheOptions, cancellationToken);
	}

	public async Task<Result<AnalyticsSummaryDto>> GetAnalyticsSummaryAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		var cached = await GetFromCacheAsync<AnalyticsSummaryDto>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			_logger.LogDebug("Returning cached analytics summary");
			return Result.Ok(cached);
		}

		var query = new GetAnalyticsSummaryQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await SetInCacheAsync(cacheKey, result.Value, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<IssuesByStatusDto>>> GetIssuesByStatusAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_status_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		var cached = await GetFromCacheAsync<List<IssuesByStatusDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			_logger.LogDebug("Returning cached issues by status");
			return Result.Ok<IReadOnlyList<IssuesByStatusDto>>(cached);
		}

		var query = new GetIssuesByStatusQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await SetInCacheAsync(cacheKey, result.Value, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<IssuesByCategoryDto>>> GetIssuesByCategoryAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_category_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		var cached = await GetFromCacheAsync<List<IssuesByCategoryDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			_logger.LogDebug("Returning cached issues by category");
			return Result.Ok<IReadOnlyList<IssuesByCategoryDto>>(cached);
		}

		var query = new GetIssuesByCategoryQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await SetInCacheAsync(cacheKey, result.Value, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<IssuesOverTimeDto>>> GetIssuesOverTimeAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_overtime_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		var cached = await GetFromCacheAsync<List<IssuesOverTimeDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			_logger.LogDebug("Returning cached issues over time");
			return Result.Ok<IReadOnlyList<IssuesOverTimeDto>>(cached);
		}

		var query = new GetIssuesOverTimeQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await SetInCacheAsync(cacheKey, result.Value, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<ResolutionTimeDto>>> GetResolutionTimesAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_resolution_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		var cached = await GetFromCacheAsync<List<ResolutionTimeDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			_logger.LogDebug("Returning cached resolution times");
			return Result.Ok<IReadOnlyList<ResolutionTimeDto>>(cached);
		}

		var query = new GetResolutionTimesQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await SetInCacheAsync(cacheKey, result.Value, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<TopContributorDto>>> GetTopContributorsAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		int topCount = 10,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_contributors_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{topCount}";

		var cached = await GetFromCacheAsync<List<TopContributorDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			_logger.LogDebug("Returning cached top contributors");
			return Result.Ok<IReadOnlyList<TopContributorDto>>(cached);
		}

		var query = new GetTopContributorsQuery(startDate, endDate, topCount);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await SetInCacheAsync(cacheKey, result.Value, cancellationToken);
		}

		return result;
	}

	public async Task<Result<byte[]>> ExportAnalyticsAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		// No caching for exports
		var query = new ExportAnalyticsQuery(startDate, endDate);
		return await _mediator.Send(query, cancellationToken);
	}
}
