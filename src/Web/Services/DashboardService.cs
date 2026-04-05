// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     DashboardService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Dashboard.Queries;

using MediatR;

namespace Web.Services;

/// <summary>
///   Service facade for Dashboard operations, wrapping MediatR calls.
/// </summary>
public interface IDashboardService
{
	/// <summary>
	///   Gets dashboard data for the specified user.
	/// </summary>
	Task<Result<UserDashboardDto>> GetUserDashboardAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of IDashboardService using MediatR with cache-aside reads
///   (5-minute TTL per user).
///
///   Dashboard entries expire after <see cref="DashboardCacheTtl"/> (5 minutes).
///   Write-side invalidation is not implemented because DashboardService has no
///   write methods.  Dashboard counts may lag issue mutations by up to 5 minutes;
///   this is an accepted trade-off.  If real-time accuracy is required, consider
///   embedding the <c>issues_version</c> counter into the dashboard cache key so
///   that <see cref="IIssueService"/> write operations invalidate it implicitly.
/// </summary>
public sealed class DashboardService : IDashboardService
{
	private const string DashboardKeyPrefix = "dashboard_user_";
	private static readonly TimeSpan DashboardCacheTtl = TimeSpan.FromMinutes(5);

	private readonly IMediator _mediator;
	private readonly DistributedCacheHelper _cache;

	public DashboardService(IMediator mediator, DistributedCacheHelper cache)
	{
		_mediator = mediator;
		_cache = cache;
	}

	public async Task<Result<UserDashboardDto>> GetUserDashboardAsync(
		string userId,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{DashboardKeyPrefix}{userId}";

		var cached = await _cache.GetAsync<UserDashboardDto>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok(cached);
		}

		var query = new GetUserDashboardQuery(userId);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await _cache.SetAsync(cacheKey, result.Value, DashboardCacheTtl, cancellationToken);
		}

		return result;
	}
}
