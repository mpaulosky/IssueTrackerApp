// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using System.Buffers.Binary;
using System.Text.Json;

using Auth0.ManagementApi;
using Auth0.ManagementApi.Users;

using Domain.Abstractions;
using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Web.Features.Admin.Users;

/// <summary>
///   Implements <see cref="IUserManagementService" /> using the Auth0 Management API v8.
/// </summary>
/// <remarks>
///   Credentials are managed by the injected <see cref="IManagementApiClient" />, which uses
///   <c>ClientCredentialsTokenProvider</c> to handle M2M token acquisition and caching internally.
///   Role IDs are resolved dynamically by name and cached for 30 minutes so they are never hardcoded.
///   <para>
///     <b>Rate limits:</b> Auth0 Management API returns HTTP 429 on burst.  Add a Polly retry
///     policy (per ADR #130) in a follow-up task.
///   </para>
/// </remarks>
public sealed class UserManagementService : IUserManagementService
{
	// ── IMemoryCache key (role-ID map) ────────────────────────────────────────
	private const string RolesCacheKey = "Auth0Management:Roles";

	// ── IDistributedCache keys (result data — Sprint 2) ──────────────────────
	private const string UserListCacheKeyPrefix = "auth0_users_page_";
	private const string UserByIdCacheKeyPrefix  = "auth0_user_";
	private const string RolesListCacheKey       = "auth0_roles_list";
	private const string UserListVersionKey      = "auth0_users_version";

	// ── TTLs ──────────────────────────────────────────────────────────────────
	private static readonly TimeSpan UserListTtl   = TimeSpan.FromMinutes(5);
	private static readonly TimeSpan UserByIdTtl   = TimeSpan.FromMinutes(10);
	private static readonly TimeSpan RolesListTtl  = TimeSpan.FromMinutes(30);

	private readonly IMemoryCache _cache;
	private readonly IDistributedCache _distributedCache;
	private readonly IManagementApiClient _managementClient;
	private readonly ILogger<UserManagementService> _logger;

	/// <summary>
	///   Initializes a new instance of <see cref="UserManagementService" />.
	/// </summary>
	public UserManagementService(
		IMemoryCache cache,
		IDistributedCache distributedCache,
		IManagementApiClient managementClient,
		ILogger<UserManagementService> logger)
	{
		_cache = cache;
		_distributedCache = distributedCache;
		_managementClient = managementClient;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task<Result<IReadOnlyList<AdminUserSummary>>> ListUsersAsync(
		int page,
		int perPage,
		CancellationToken ct)
	{
		try
		{
			var version = await GetUserListVersionAsync(ct).ConfigureAwait(false);
			var cacheKey = $"{UserListCacheKeyPrefix}{version}_{page}_{perPage}";

			var cached = await GetFromDistributedCacheAsync<List<AdminUserSummary>>(cacheKey, ct)
				.ConfigureAwait(false);

			if (cached is not null)
			{
				_logger.LogDebug(
					"Returning cached user list. Page={Page}, PerPage={PerPage}, Version={Version}",
					page, perPage, version);
				return Result.Ok<IReadOnlyList<AdminUserSummary>>(cached);
			}



			// Auth0 uses 0-based page numbering; callers pass 1-based pages.
			var auth0Page = Math.Max(0, page - 1);
			var pager = await _managementClient.Users
				.ListAsync(new ListUsersRequestParameters { Page = auth0Page, PerPage = perPage }, null, ct)
				.ConfigureAwait(false);

			var users = pager.CurrentPage.Items;

			// Auth0's list endpoint does not include role assignments; fetch them per user in
			// parallel to avoid sequential N+1 latency.
			var summaries = await Task.WhenAll(users.Select(async u =>
			{
				if (string.IsNullOrWhiteSpace(u.UserId))
				{
					_logger.LogWarning(
						"Skipping Auth0 role lookup for listed user with missing UserId. Email={Email}",
						u.Email ?? string.Empty);
					return MapUser(u) with { UserId = string.Empty };
				}

				var rolesPager = await _managementClient.Users.Roles
					.ListAsync(u.UserId, new ListUserRolesRequestParameters { PerPage = 100 }, null, ct)
					.ConfigureAwait(false);

				return MapUser(u) with
				{
					Roles = rolesPager.CurrentPage.Items.Select(r => r.Name ?? string.Empty).ToList()
				};
			})).ConfigureAwait(false);

			var result = summaries.ToList();
			await SetInDistributedCacheAsync(cacheKey, result, UserListTtl, ct).ConfigureAwait(false);

			return Result.Ok<IReadOnlyList<AdminUserSummary>>(result);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(
				ex,
				"Failed to list users from Auth0. Page={Page}, PerPage={PerPage}",
				page,
				perPage);

			return Result.Fail<IReadOnlyList<AdminUserSummary>>(
				$"Failed to retrieve users: {ex.Message}",
				ResultErrorCode.ExternalService);
		}
	}

	/// <inheritdoc />
	public async Task<Result<AdminUserSummary>> GetUserByIdAsync(
		string userId,
		CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			return Result.Fail<AdminUserSummary>(
				"User ID must not be empty.",
				ResultErrorCode.Validation);
		}

		try
		{
			var cacheKey = $"{UserByIdCacheKeyPrefix}{userId}";

			var cached = await GetFromDistributedCacheAsync<AdminUserSummary>(cacheKey, ct)
				.ConfigureAwait(false);

			if (cached is not null)
			{
				_logger.LogDebug("Returning cached user. UserId={UserId}", userId);
				return Result.Ok(cached);
			}


			var user = await _managementClient.Users
				.GetAsync(userId, new GetUserRequestParameters(), null, ct)
				.ConfigureAwait(false);

			// Fetch the roles assigned to this user (requires a separate API call).
			var rolesPager = await _managementClient.Users.Roles
				.ListAsync(userId, new ListUserRolesRequestParameters { PerPage = 100 }, null, ct)
				.ConfigureAwait(false);

			var summary = MapUser(user) with
			{
				Roles = rolesPager.CurrentPage.Items.Select(r => r.Name ?? string.Empty).ToList()
			};

			await SetInDistributedCacheAsync(cacheKey, summary, UserByIdTtl, ct).ConfigureAwait(false);

			return Result.Ok(summary);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Failed to retrieve user from Auth0. UserId={UserId}", userId);

			return Result.Fail<AdminUserSummary>(
				$"Failed to retrieve user '{userId}': {ex.Message}",
				ResultErrorCode.ExternalService);
		}
	}

	/// <inheritdoc />
	public async Task<Result<bool>> AssignRolesAsync(
		string userId,
		IEnumerable<string> roleNames,
		CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			return Result.Fail<bool>("User ID must not be empty.", ResultErrorCode.Validation);
		}

		var roleNamesList = (roleNames ?? []).ToList();
		if (roleNamesList.Count == 0)
		{
			return Result.Ok(true);
		}

		try
		{
			var roleMap = await GetRoleMapAsync(ct).ConfigureAwait(false);

			var unknown = roleNamesList.Where(r => !roleMap.ContainsKey(r)).ToList();
			if (unknown.Count > 0)
			{
				return Result.Fail<bool>(
					$"Unknown role(s): {string.Join(", ", unknown)}",
					ResultErrorCode.Validation);
			}

			var roleIds = roleNamesList.Select(r => roleMap[r]).ToArray();

			await _managementClient.Users.Roles
				.AssignAsync(userId, new AssignUserRolesRequestContent { Roles = roleIds }, null, ct)
				.ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(
				ex,
				"Failed to assign roles to user. UserId={UserId}, Roles={Roles}",
				userId,
				string.Join(", ", roleNamesList));

			return Result.Fail<bool>(
				$"Failed to assign roles: {ex.Message}",
				ResultErrorCode.ExternalService);
		}

		// Auth0 commit succeeded — invalidate cache outside the Auth0 try/catch so a
		// Redis failure is logged as a warning but never rolls back a successful role change.
		try
		{
			await _distributedCache
				.RemoveAsync($"{UserByIdCacheKeyPrefix}{userId}", ct)
				.ConfigureAwait(false);
			await BumpUserListVersionAsync(ct).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(
				ex,
				"Cache invalidation failed after AssignRolesAsync for UserId={UserId}. " +
				"Stale data may be served until TTL expires.",
				userId);
		}

		return Result.Ok(true);
	}

	/// <inheritdoc />
	public async Task<Result<bool>> RemoveRolesAsync(
		string userId,
		IEnumerable<string> roleNames,
		CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			return Result.Fail<bool>("User ID must not be empty.", ResultErrorCode.Validation);
		}

		var roleNamesList = (roleNames ?? []).ToList();
		if (roleNamesList.Count == 0)
		{
			return Result.Ok(true);
		}

		try
		{
			var roleMap = await GetRoleMapAsync(ct).ConfigureAwait(false);

			var unknown = roleNamesList.Where(r => !roleMap.ContainsKey(r)).ToList();
			if (unknown.Count > 0)
			{
				return Result.Fail<bool>(
					$"Unknown role(s): {string.Join(", ", unknown)}",
					ResultErrorCode.Validation);
			}

			var roleIds = roleNamesList.Select(r => roleMap[r]).ToArray();

			await _managementClient.Users.Roles
				.DeleteAsync(userId, new DeleteUserRolesRequestContent { Roles = roleIds }, null, ct)
				.ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(
				ex,
				"Failed to remove roles from user. UserId={UserId}, Roles={Roles}",
				userId,
				string.Join(", ", roleNamesList));

			return Result.Fail<bool>(
				$"Failed to remove roles: {ex.Message}",
				ResultErrorCode.ExternalService);
		}

		// Auth0 commit succeeded — invalidate cache outside the Auth0 try/catch.
		try
		{
			await _distributedCache
				.RemoveAsync($"{UserByIdCacheKeyPrefix}{userId}", ct)
				.ConfigureAwait(false);
			await BumpUserListVersionAsync(ct).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(
				ex,
				"Cache invalidation failed after RemoveRolesAsync for UserId={UserId}. " +
				"Stale data may be served until TTL expires.",
				userId);
		}

		return Result.Ok(true);
	}

	/// <inheritdoc />
	public async Task<Result<IReadOnlyList<RoleAssignment>>> ListRolesAsync(CancellationToken ct)
	{
		try
		{
			var cached = await GetFromDistributedCacheAsync<List<RoleAssignment>>(RolesListCacheKey, ct)
				.ConfigureAwait(false);

			if (cached is not null)
			{
				_logger.LogDebug("Returning cached roles list.");
				return Result.Ok<IReadOnlyList<RoleAssignment>>(cached);
			}


			var pager = await _managementClient.Roles
				.ListAsync(new ListRolesRequestParameters { PerPage = 100 }, null, ct)
				.ConfigureAwait(false);

			var result = pager.CurrentPage.Items
				.Select(r => new RoleAssignment
				{
					RoleId = r.Id ?? string.Empty,
					RoleName = r.Name ?? string.Empty,
					Description = r.Description ?? string.Empty
				})
				.ToList();

			await SetInDistributedCacheAsync(RolesListCacheKey, result, RolesListTtl, ct)
				.ConfigureAwait(false);

			return Result.Ok<IReadOnlyList<RoleAssignment>>(result);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Failed to list roles from Auth0.");

			return Result.Fail<IReadOnlyList<RoleAssignment>>(
				$"Failed to retrieve roles: {ex.Message}",
				ResultErrorCode.ExternalService);
		}
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Private helpers
	// ──────────────────────────────────────────────────────────────────────────

	/// <summary>
	///   Attempts to deserialize a value from <see cref="IDistributedCache" />.
	///   Returns <see langword="null" /> on miss or deserialization failure.
	/// </summary>
	private async Task<T?> GetFromDistributedCacheAsync<T>(string key, CancellationToken ct)
	{
		try
		{
			var bytes = await _distributedCache.GetAsync(key, ct).ConfigureAwait(false);
			return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Distributed cache read failed for key '{Key}'. Treating as miss.", key);
			return default;
		}
	}

	/// <summary>
	///   Serializes and stores a value in <see cref="IDistributedCache" /> with the given TTL.
	///   Errors are logged as warnings so a cache write failure never breaks the caller.
	/// </summary>
	private async Task SetInDistributedCacheAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
	{
		try
		{
			var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
			await _distributedCache.SetAsync(
				key,
				bytes,
				new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
				ct).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Distributed cache write failed for key '{Key}'. Continuing without cache.", key);
		}
	}

	/// <summary>
	///   Returns the current user-list cache version counter.
	///   Returns 0 when no version entry exists yet (cold start).
	///   On a cache read error returns a sentinel (<see cref="long.MinValue" />) that cannot
	///   match any previously written paginated-list key, guaranteeing a cache miss rather than
	///   accidentally serving stale data under a real but lower-numbered version key.
	/// </summary>
	private async Task<long> GetUserListVersionAsync(CancellationToken ct)
	{
		try
		{
			var bytes = await _distributedCache.GetAsync(UserListVersionKey, ct).ConfigureAwait(false);
			if (bytes is null) return 0L;
			return BinaryPrimitives.ReadInt64LittleEndian(bytes);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// Return a sentinel that will never match an existing cache key, forcing a live
			// Auth0 call. Do NOT return 0: version-0 paginated entries from before the first
			// role change may still be in cache and would be incorrectly served as hits.
			_logger.LogWarning(ex,
				"Failed to read user-list version from distributed cache. Using sentinel to force cache miss.");
			return long.MinValue;
		}
	}

	/// <summary>
	///   Increments the user-list version counter in <see cref="IDistributedCache" /> using an
	///   endian-stable encoding, which logically invalidates all existing paginated list entries.
	///   The version key TTL is set to <c>UserListTtl + 1 minute</c> so the key always outlives
	///   any paginated entry it governs, preventing Redis LRU eviction from resurrecting stale
	///   version-0 entries.
	/// </summary>
	private async Task BumpUserListVersionAsync(CancellationToken ct)
	{
		// Throws on error — callers wrap this in their own try/catch with appropriate logging.
		var current = await GetUserListVersionAsync(ct).ConfigureAwait(false);
		// If GetUserListVersionAsync returned the sentinel, start from 1 so version 0 entries
		// (which may still be in cache) are never matched.
		var nextValue = current == long.MinValue ? 1L : current + 1L;
		var next = new byte[sizeof(long)];
		BinaryPrimitives.WriteInt64LittleEndian(next, nextValue);

		// TTL must exceed UserListTtl so the key is never evicted while paginated entries live.
		await _distributedCache.SetAsync(
			UserListVersionKey,
			next,
			new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = UserListTtl + TimeSpan.FromMinutes(1)
			},
			ct).ConfigureAwait(false);

		_logger.LogDebug("User-list cache version bumped to {Version}.", nextValue);
	}

	/// <summary>
	///   Returns a name → ID map of all tenant roles, backed by a 30-minute in-memory cache.
	///   Uses <see cref="IMemoryCache.GetOrCreateAsync{TItem}" /> to avoid race conditions
	///   on concurrent cold starts.
	/// </summary>
	private async Task<Dictionary<string, string>> GetRoleMapAsync(CancellationToken ct)
	{
		var map = await _cache.GetOrCreateAsync(RolesCacheKey, async entry =>
		{
			var pager = await _managementClient.Roles
				.ListAsync(new ListRolesRequestParameters { PerPage = 100 }, null, ct)
				.ConfigureAwait(false);

			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

			return pager.CurrentPage.Items
				.Where(r => r.Name is not null && r.Id is not null)
				.ToDictionary(r => r.Name!, r => r.Id!, StringComparer.OrdinalIgnoreCase);
		}).ConfigureAwait(false);

		return map ?? [];
	}

	/// <summary>Maps an Auth0 <see cref="UserResponseSchema" /> (list result) to <see cref="AdminUserSummary" />.</summary>
	private static AdminUserSummary MapUser(UserResponseSchema user) => new()
	{
		UserId = user.UserId ?? string.Empty,
		Email = user.Email ?? string.Empty,
		Name = user.Name ?? user.Email ?? string.Empty,
		Picture = user.Picture ?? string.Empty,
		Roles = [],
		LastLogin = ParseLastLogin(user.LastLogin),
		IsBlocked = user.Blocked ?? false
	};

	/// <summary>Maps an Auth0 <see cref="GetUserResponseContent" /> (single-user result) to <see cref="AdminUserSummary" />.</summary>
	private static AdminUserSummary MapUser(GetUserResponseContent user) => new()
	{
		UserId = user.UserId ?? string.Empty,
		Email = user.Email ?? string.Empty,
		Name = user.Name ?? user.Email ?? string.Empty,
		Picture = user.Picture ?? string.Empty,
		Roles = [],
		LastLogin = ParseLastLogin(user.LastLogin),
		IsBlocked = user.Blocked ?? false
	};

	/// <summary>
	///   Safely converts a <see cref="UserDateSchema" /> last-login field to a nullable
	///   <see cref="DateTimeOffset" />, returning <see langword="null" /> if the value is absent
	///   or unparseable.
	/// </summary>
	private static DateTimeOffset? ParseLastLogin(UserDateSchema? lastLogin)
	{
		if (lastLogin is null) return null;
		return lastLogin.TryGetString(out var s) && DateTimeOffset.TryParse(s, out var dto) ? dto : null;
	}
}
