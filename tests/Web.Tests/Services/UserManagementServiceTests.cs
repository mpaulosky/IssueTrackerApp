// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Text;
using System.Text.Json;

using Domain.Abstractions;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Features.Admin.Users;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for <see cref="UserManagementService" />.
/// </summary>
/// <remarks>
///   <para>
///     <b>Test coverage note:</b> <see cref="UserManagementService" /> directly instantiates
///     <see cref="Auth0.ManagementApi.ManagementApiClient" /> with a hardcoded connection, making it
///     impossible to intercept Management API HTTP calls in pure unit tests. As a result:
///     <list type="bullet">
///       <item>Input-validation paths (empty userId, empty roles) are fully covered here.</item>
///       <item>M2M token-caching behaviour is covered via <see cref="IHttpClientFactory" /> call-count assertions.</item>
///       <item>
///         Success paths that require a real Management API response (ListUsersAsync success,
///         AssignRolesAsync success) require integration tests or a refactor to inject an
///         <c>IManagementApiClientFactory</c> / <c>HttpClientManagementConnection</c>. See TODO comments.
///       </item>
///     </list>
///   </para>
/// </remarks>
public sealed class UserManagementServiceTests
{
	private static Auth0ManagementOptions DefaultOptions => new()
	{
		ClientId = "test-client-id",
		ClientSecret = "test-client-secret",
		Domain = "test-tenant.auth0.com",
		Audience = "https://test-tenant.auth0.com/api/v2/"
	};

	private static UserManagementService CreateSut(
		IMemoryCache? cache = null,
		IDistributedCache? distributedCache = null,
		IHttpClientFactory? httpClientFactory = null,
		Auth0ManagementOptions? options = null,
		ILogger<UserManagementService>? logger = null)
	{
		return new UserManagementService(
			cache ?? new MemoryCache(new MemoryCacheOptions()),
			distributedCache ?? Substitute.For<IDistributedCache>(),
			httpClientFactory ?? Substitute.For<IHttpClientFactory>(),
			Options.Create(options ?? DefaultOptions),
			logger ?? Substitute.For<ILogger<UserManagementService>>());
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Input validation — AssignRolesAsync
	// ──────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task AssignRolesAsync_EmptyUserId_ReturnsValidationFailure()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var result = await sut.AssignRolesAsync(string.Empty, ["Admin"], CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Contain("User ID");
	}

	[Fact]
	public async Task AssignRolesAsync_WhitespaceUserId_ReturnsValidationFailure()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var result = await sut.AssignRolesAsync("   ", ["Admin"], CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task AssignRolesAsync_EmptyRolesList_ReturnsImmediateSuccess()
	{
		// Arrange — no HttpClientFactory call expected because roles list is empty
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		var sut = CreateSut(httpClientFactory: httpClientFactory);

		// Act
		var result = await sut.AssignRolesAsync("auth0|user1", [], CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
		httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
	}

	[Fact]
	public async Task AssignRolesAsync_NullRolesList_ReturnsImmediateSuccess()
	{
		// Arrange
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		var sut = CreateSut(httpClientFactory: httpClientFactory);

		// Act
		var result = await sut.AssignRolesAsync("auth0|user1", null!, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Input validation — RemoveRolesAsync
	// ──────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task RemoveRolesAsync_EmptyUserId_ReturnsValidationFailure()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var result = await sut.RemoveRolesAsync(string.Empty, ["Admin"], CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Contain("User ID");
	}

	[Fact]
	public async Task RemoveRolesAsync_EmptyRolesList_ReturnsImmediateSuccess()
	{
		// Arrange — no HttpClientFactory call expected
		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		var sut = CreateSut(httpClientFactory: httpClientFactory);

		// Act
		var result = await sut.RemoveRolesAsync("auth0|user1", [], CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
		httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Input validation — GetUserByIdAsync
	// ──────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task GetUserByIdAsync_EmptyUserId_ReturnsValidationFailure()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var result = await sut.GetUserByIdAsync(string.Empty, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	// ──────────────────────────────────────────────────────────────────────────
	// M2M Token caching
	// ──────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task ListUsersAsync_TokenFetchedOnFirstCall_CachedTokenUsedOnSecondCall()
	{
		// Arrange — use a fake HTTP handler that intercepts the token-endpoint call.
		// ManagementApiClient creates its own HttpClient and will fail to connect to the
		// fake domain (ExternalService), but the IHttpClientFactory call-count tells us
		// whether the token was re-fetched.
		var tokenCallCount = 0;
		var fakeTokenHandler = new FakeHttpMessageHandler(request =>
		{
			tokenCallCount++;
			var json = JsonSerializer.Serialize(new
			{
				access_token = "fake-management-token",
				token_type = "Bearer",
				expires_in = 86400
			});
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
		});

		var httpClientFactory = Substitute.For<IHttpClientFactory>();
		httpClientFactory.CreateClient(Arg.Any<string>())
			.Returns(new HttpClient(fakeTokenHandler));

		var sut = CreateSut(
			cache: new MemoryCache(new MemoryCacheOptions()),
			httpClientFactory: httpClientFactory);

		// Act — two consecutive calls; both will fail at Management API level (ExternalService)
		// but only the first should trigger a token fetch via IHttpClientFactory.
		var first = await sut.ListUsersAsync(1, 10, CancellationToken.None);
		var second = await sut.ListUsersAsync(1, 10, CancellationToken.None);

		// Assert — both return ExternalService (can't reach fake domain), but token was
		// fetched only once.
		first.Failure.Should().BeTrue();
		first.ErrorCode.Should().Be(ResultErrorCode.ExternalService);

		second.Failure.Should().BeTrue();
		second.ErrorCode.Should().Be(ResultErrorCode.ExternalService);

		// IHttpClientFactory.CreateClient() must have been invoked exactly once across both calls.
		httpClientFactory.Received(1).CreateClient(Arg.Any<string>());
	}

	[Fact]
	public async Task ListUsersAsync_TokenAlreadyInCache_DoesNotCallHttpClientFactory()
	{
		// Arrange — pre-populate the cache with a valid token so no HTTP call is needed.
		var cache = new MemoryCache(new MemoryCacheOptions());
		cache.Set("Auth0Management:Token", "pre-cached-token", TimeSpan.FromHours(1));

		var httpClientFactory = Substitute.For<IHttpClientFactory>();

		var sut = CreateSut(cache: cache, httpClientFactory: httpClientFactory);

		// Act
		await sut.ListUsersAsync(1, 10, CancellationToken.None);

		// Assert — factory must NOT be called because token came from cache.
		httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
	}

	// TODO: Test that an expired token (past TTL) triggers a fresh token fetch.
	// This requires injecting a time abstraction (e.g., TimeProvider) into the service
	// so tests can advance the clock past the cache TTL without waiting real time.
	// Tracked as a follow-up refactor: inject TimeProvider into UserManagementService.

	// TODO: Test ListUsersAsync success path (returns populated list).
	// TODO: Test AssignRolesAsync success path (roles assigned, returns true).
	// TODO: Test RemoveRolesAsync success path (roles removed, returns true).
	// TODO: Test Auth0 API error → ResultErrorCode.ExternalService for all methods.
	// These paths require UserManagementService to be refactored to accept an injectable
	// IManagementApiClientFactory (or HttpClientManagementConnection), so that
	// ManagementApiClient's HTTP calls can be intercepted in unit tests.

	// ──────────────────────────────────────────────────────────────────────────
	// Helpers
	// ──────────────────────────────────────────────────────────────────────────

	/// <summary>
	///   Minimal <see cref="HttpMessageHandler" /> that delegates to a synchronous lambda.
	/// </summary>
	private sealed class FakeHttpMessageHandler(
		Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
			=> Task.FromResult(handler(request));
	}
}
