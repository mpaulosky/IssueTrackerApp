// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using System.Buffers.Binary;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;

using Domain.Abstractions;
using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Web.Features.Admin.Users;

/// <summary>
///   Implements <see cref="IUserManagementService" /> using the Auth0 Management API v2.
/// </summary>
/// <remarks>
///   An M2M access token is obtained via the OAuth 2.0 client credentials flow and cached in
///   <see cref="IMemoryCache" /> with a 24 h TTL minus a 5-minute safety margin.  Role IDs are
///   resolved dynamically by name and cached for 30 minutes so they are never hardcoded.
///   <para>
///     <b>Rate limits:</b> Auth0 Management API returns HTTP 429 on burst.  Add a Polly retry
///     policy (per ADR #130) in a follow-up task once the HttpClientManagementConnection
///     integration is confirmed against the tenant's SDK version.
///   </para>
/// </remarks>
public sealed class UserManagementService : IUserManagementService
{
	// ── IMemoryCache keys (token + role-ID map — DO NOT CHANGE) ──────────────
	private const string TokenCacheKey = "Auth0Management:Token";
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
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly Auth0ManagementOptions _options;
	private readonly ILogger<UserManagementService> _logger;

	/// <summary>
	///   Initializes a new instance of <see cref="UserManagementService" />.
	/// </summary>
	public UserManagementService(
		IMemoryCache cache,
		IDistributedCache distributedCache,
		IHttpClientFactory httpClientFactory,
		IOptions<Auth0ManagementOptions> options,
		ILogger<UserManagementService> logger)
	{
		_cache = cache;
		_distributedCache = distributedCache;
		_httpClientFactory = httpClientFactory;
		_options = options.Value;
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

			using var client = await GetManagementClientAsync(ct).ConfigureAwait(false);

			// Auth0 uses 0-based page numbering; callers pass 1-based pages.
			var auth0Page = Math.Max(0, page - 1);
			var users = await client.Users
				.GetAllAsync(new GetUsersRequest(), new PaginationInfo(auth0Page, perPage, false), ct)
				.ConfigureAwait(false);

			// Auth0's list endpoint does not include role assignments; fetch them per user in
			// parallel to avoid sequential N+1 latency.
			var summaries = await Task.WhenAll(users.Select(async u =>
			{
				var roles = await client.Users
					.GetRolesAsync(u.UserId, new PaginationInfo(0, 100, false), ct)
					.ConfigureAwait(false);

				return MapUser(u) with
				{
					Roles = roles.Select(r => r.Name ?? string.Empty).ToList()
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

			using var client = await GetManagementClientAsync(ct).ConfigureAwait(false);

			var user = await client.Users
				.GetAsync(userId, cancellationToken: ct)
				.ConfigureAwait(false);

			// Fetch the roles assigned to this user (requires a separate API call).
			var rolesList = await client.Users
				.GetRolesAsync(userId, new PaginationInfo(0, 100, false), ct)
				.ConfigureAwait(false);

			var summary = MapUser(user) with
			{
				Roles = rolesList.Select(r => r.Name ?? string.Empty).ToList()
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
			using var client = await GetManagementClientAsync(ct).ConfigureAwait(false);
			var roleMap = await GetRoleMapAsync(client, ct).ConfigureAwait(false);

			var unknown = roleNamesList.Where(r => !roleMap.ContainsKey(r)).ToList();
			if (unknown.Count > 0)
			{
				return Result.Fail<bool>(
					$"Unknown role(s): {string.Join(", ", unknown)}",
					ResultErrorCode.Validation);
			}

			var roleIds = roleNamesList.Select(r => roleMap[r]).ToArray();

			await client.Users
				.AssignRolesAsync(userId, new AssignRolesRequest { Roles = roleIds }, ct)
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
			using var client = await GetManagementClientAsync(ct).ConfigureAwait(false);
			var roleMap = await GetRoleMapAsync(client, ct).ConfigureAwait(false);

			var unknown = roleNamesList.Where(r => !roleMap.ContainsKey(r)).ToList();
			if (unknown.Count > 0)
			{
				return Result.Fail<bool>(
					$"Unknown role(s): {string.Join(", ", unknown)}",
					ResultErrorCode.Validation);
			}

			var roleIds = roleNamesList.Select(r => roleMap[r]).ToArray();

			await client.Users
				.RemoveRolesAsync(userId, new AssignRolesRequest { Roles = roleIds }, ct)
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

			using var client = await GetManagementClientAsync(ct).ConfigureAwait(false);

			var roles = await client.Roles
				.GetAllAsync(new GetRolesRequest(), new PaginationInfo(0, 100, false), ct)
				.ConfigureAwait(false);

			var result = roles
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
	///   Creates a <see cref="ManagementApiClient" /> using a cached M2M access token.
	/// </summary>
	private async Task<ManagementApiClient> GetManagementClientAsync(CancellationToken ct)
	{
		var token = await GetOrFetchTokenAsync(ct).ConfigureAwait(false);
		return new ManagementApiClient(token, new Uri($"https://{_options.Domain}/api/v2/"));
	}

	/// <summary>
	///   Returns a cached M2M access token, fetching a fresh one from Auth0 when expired.
	///   Uses <see cref="IMemoryCache.GetOrCreateAsync{TItem}" /> to avoid concurrent cold-start
	///   races where multiple in-flight requests each fetch a new token simultaneously.
	/// </summary>
	private async Task<string> GetOrFetchTokenAsync(CancellationToken ct)
	{
		var token = await _cache.GetOrCreateAsync(TokenCacheKey, async entry =>
		{
			_logger.LogDebug(
				"Fetching fresh Auth0 Management API token for domain '{Domain}'.",
				_options.Domain);

			using var httpClient = _httpClientFactory.CreateClient();

			using var requestBody = new FormUrlEncodedContent(
			[
				new KeyValuePair<string, string>("grant_type", "client_credentials"),
				new KeyValuePair<string, string>("client_id", _options.ClientId),
				new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
				new KeyValuePair<string, string>("audience", _options.Audience)
			]);

			using var response = await httpClient
				.PostAsync($"https://{_options.Domain}/oauth/token", requestBody, ct)
				.ConfigureAwait(false);

			response.EnsureSuccessStatusCode();

			var tokenResponse = await response.Content
				.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
				.ConfigureAwait(false)
				?? throw new InvalidOperationException(
					"Auth0 token endpoint returned an empty response.");

			// Cache with a 5-minute safety margin so we always have time to act on the token.
			var ttl = tokenResponse.ExpiresIn > 300
				? tokenResponse.ExpiresIn - 300
				: tokenResponse.ExpiresIn;

			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl);

			_logger.LogDebug("Auth0 Management API token cached. TTL={Ttl}s.", ttl);

			return tokenResponse.AccessToken;
		}).ConfigureAwait(false);

		return token ?? throw new InvalidOperationException(
			"Auth0 token cache returned null — token fetch may have failed.");
	}

	/// <summary>
	///   Returns a name → ID map of all tenant roles, backed by a 30-minute cache.
	///   Uses <see cref="IMemoryCache.GetOrCreateAsync{TItem}" /> to avoid race conditions
	///   on concurrent cold starts.
	/// </summary>
	private async Task<Dictionary<string, string>> GetRoleMapAsync(
		ManagementApiClient client,
		CancellationToken ct)
	{
		var map = await _cache.GetOrCreateAsync(RolesCacheKey, async entry =>
		{
			var roles = await client.Roles
				.GetAllAsync(new GetRolesRequest(), new PaginationInfo(0, 100, false), ct)
				.ConfigureAwait(false);

			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

			return roles
				.Where(r => r.Name is not null && r.Id is not null)
				.ToDictionary(r => r.Name!, r => r.Id!, StringComparer.OrdinalIgnoreCase);
		}).ConfigureAwait(false);

		return map ?? [];
	}

	/// <summary>Maps an Auth0 <see cref="User" /> to <see cref="AdminUserSummary" />.</summary>
	private static AdminUserSummary MapUser(User user) => new()
	{
		UserId = user.UserId ?? string.Empty,
		Email = user.Email ?? string.Empty,
		Name = user.FullName ?? user.Email ?? string.Empty,
		Picture = user.Picture ?? string.Empty,
		Roles = [],
		LastLogin = user.LastLogin is { } lastLogin
			? new DateTimeOffset(DateTime.SpecifyKind(lastLogin, DateTimeKind.Utc))
			: null,
		IsBlocked = user.Blocked ?? false
	};

	/// <summary>Thin DTO for deserializing the Auth0 token endpoint response.</summary>
	private sealed record TokenResponse(
		[property: JsonPropertyName("access_token")] string AccessToken,
		[property: JsonPropertyName("token_type")] string TokenType,
		[property: JsonPropertyName("expires_in")] int ExpiresIn);
}
