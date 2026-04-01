// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;

using Domain.Abstractions;
using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

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
	private const string TokenCacheKey = "Auth0Management:Token";
	private const string RolesCacheKey = "Auth0Management:Roles";

	private readonly IMemoryCache _cache;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly Auth0ManagementOptions _options;
	private readonly ILogger<UserManagementService> _logger;

	/// <summary>
	///   Initializes a new instance of <see cref="UserManagementService" />.
	/// </summary>
	public UserManagementService(
		IMemoryCache cache,
		IHttpClientFactory httpClientFactory,
		IOptions<Auth0ManagementOptions> options,
		ILogger<UserManagementService> logger)
	{
		_cache = cache;
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
			using var client = await GetManagementClientAsync(ct).ConfigureAwait(false);

			// Auth0 uses 0-based page numbering; callers pass 1-based pages.
			var auth0Page = Math.Max(0, page - 1);
			var users = await client.Users
				.GetAllAsync(new GetUsersRequest(), new PaginationInfo(auth0Page, perPage, false), ct)
				.ConfigureAwait(false);

			var summaries = users.Select(MapUser).ToList();
			return Result.Ok<IReadOnlyList<AdminUserSummary>>(summaries);
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

			return Result.Ok(true);
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

			return Result.Ok(true);
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
	}

	/// <inheritdoc />
	public async Task<Result<IReadOnlyList<RoleAssignment>>> ListRolesAsync(CancellationToken ct)
	{
		try
		{
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
	///   Creates a <see cref="ManagementApiClient" /> using a cached M2M access token.
	/// </summary>
	private async Task<ManagementApiClient> GetManagementClientAsync(CancellationToken ct)
	{
		var token = await GetOrFetchTokenAsync(ct).ConfigureAwait(false);
		return new ManagementApiClient(token, new Uri($"https://{_options.Domain}/api/v2/"));
	}

	/// <summary>
	///   Returns a cached M2M access token, fetching a fresh one from Auth0 when expired.
	/// </summary>
	private async Task<string> GetOrFetchTokenAsync(CancellationToken ct)
	{
		if (_cache.TryGetValue(TokenCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
		{
			return cached;
		}

		_logger.LogDebug("Fetching fresh Auth0 Management API token for domain '{Domain}'.", _options.Domain);

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

		_cache.Set(TokenCacheKey, tokenResponse.AccessToken, TimeSpan.FromSeconds(ttl));

		_logger.LogDebug("Auth0 Management API token cached. TTL={Ttl}s.", ttl);

		return tokenResponse.AccessToken;
	}

	/// <summary>
	///   Returns a name → ID map of all tenant roles, backed by a 30-minute cache.
	/// </summary>
	private async Task<Dictionary<string, string>> GetRoleMapAsync(
		ManagementApiClient client,
		CancellationToken ct)
	{
		if (_cache.TryGetValue(RolesCacheKey, out Dictionary<string, string>? map) && map is not null)
		{
			return map;
		}

		var roles = await client.Roles
			.GetAllAsync(new GetRolesRequest(), new PaginationInfo(0, 100, false), ct)
			.ConfigureAwait(false);

		var roleMap = roles
			.Where(r => r.Name is not null && r.Id is not null)
			.ToDictionary(r => r.Name!, r => r.Id!, StringComparer.OrdinalIgnoreCase);

		_cache.Set(RolesCacheKey, roleMap, TimeSpan.FromMinutes(30));

		return roleMap;
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
