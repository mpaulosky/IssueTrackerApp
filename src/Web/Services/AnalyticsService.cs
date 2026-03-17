// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AnalyticsService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs.Analytics;
using Domain.Features.Analytics.Queries;

using MediatR;

using Microsoft.Extensions.Caching.Memory;

namespace Web.Services;

/// <summary>
/// Service implementation for analytics operations.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
	private readonly IMediator _mediator;
	private readonly IMemoryCache _cache;
	private readonly ILogger<AnalyticsService> _logger;
	private const int CacheExpirationMinutes = 5;

	public AnalyticsService(
		IMediator mediator,
		IMemoryCache cache,
		ILogger<AnalyticsService> logger)
	{
		_mediator = mediator;
		_cache = cache;
		_logger = logger;
	}

	public async Task<Result<AnalyticsSummaryDto>> GetAnalyticsSummaryAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		if (_cache.TryGetValue(cacheKey, out Result<AnalyticsSummaryDto>? cachedResult) && cachedResult != null)
		{
			_logger.LogDebug("Returning cached analytics summary");
			return cachedResult;
		}

		var query = new GetAnalyticsSummaryQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success)
		{
			_cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<IssuesByStatusDto>>> GetIssuesByStatusAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_status_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		if (_cache.TryGetValue(cacheKey, out Result<IReadOnlyList<IssuesByStatusDto>>? cachedResult) && cachedResult != null)
		{
			_logger.LogDebug("Returning cached issues by status");
			return cachedResult;
		}

		var query = new GetIssuesByStatusQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success)
		{
			_cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<IssuesByCategoryDto>>> GetIssuesByCategoryAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_category_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		if (_cache.TryGetValue(cacheKey, out Result<IReadOnlyList<IssuesByCategoryDto>>? cachedResult) && cachedResult != null)
		{
			_logger.LogDebug("Returning cached issues by category");
			return cachedResult;
		}

		var query = new GetIssuesByCategoryQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success)
		{
			_cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<IssuesOverTimeDto>>> GetIssuesOverTimeAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_overtime_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		if (_cache.TryGetValue(cacheKey, out Result<IReadOnlyList<IssuesOverTimeDto>>? cachedResult) && cachedResult != null)
		{
			_logger.LogDebug("Returning cached issues over time");
			return cachedResult;
		}

		var query = new GetIssuesOverTimeQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success)
		{
			_cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));
		}

		return result;
	}

	public async Task<Result<IReadOnlyList<ResolutionTimeDto>>> GetResolutionTimesAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"analytics_resolution_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

		if (_cache.TryGetValue(cacheKey, out Result<IReadOnlyList<ResolutionTimeDto>>? cachedResult) && cachedResult != null)
		{
			_logger.LogDebug("Returning cached resolution times");
			return cachedResult;
		}

		var query = new GetResolutionTimesQuery(startDate, endDate);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success)
		{
			_cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));
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

		if (_cache.TryGetValue(cacheKey, out Result<IReadOnlyList<TopContributorDto>>? cachedResult) && cachedResult != null)
		{
			_logger.LogDebug("Returning cached top contributors");
			return cachedResult;
		}

		var query = new GetTopContributorsQuery(startDate, endDate, topCount);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success)
		{
			_cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));
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
