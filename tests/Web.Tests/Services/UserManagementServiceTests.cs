// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Runtime.CompilerServices;

using Auth0.ManagementApi;
using Auth0.ManagementApi.Core;
using Auth0.ManagementApi.Users;

using Domain.Abstractions;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using Web.Features.Admin.Users;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for <see cref="UserManagementService" />.
/// </summary>
/// <remarks>
///   <para>
///     <b>Test coverage note:</b> <see cref="UserManagementService" /> uses an injected
///     <see cref="IManagementApiClient" />, allowing all Management API calls to be intercepted
///     via NSubstitute. Coverage includes:
///     <list type="bullet">
///       <item>Input-validation paths (empty userId, empty roles).</item>
///       <item>Early-exit paths (empty roles list, whitespace userId).</item>
///     </list>
///   </para>
/// </remarks>
public sealed class UserManagementServiceTests
{
	private static UserManagementService CreateSut(
		IMemoryCache? cache = null,
		IDistributedCache? distributedCache = null,
		IManagementApiClient? managementApiClient = null,
		ILogger<UserManagementService>? logger = null)
	{
		return new UserManagementService(
			cache ?? new MemoryCache(new MemoryCacheOptions()),
			distributedCache ?? Substitute.For<IDistributedCache>(),
			managementApiClient ?? Substitute.For<IManagementApiClient>(),
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
		// Arrange — no Management API call expected because roles list is empty
		var managementClient = Substitute.For<IManagementApiClient>();
		var sut = CreateSut(managementApiClient: managementClient);

		// Act
		var result = await sut.AssignRolesAsync("auth0|user1", [], CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
		managementClient.ReceivedCalls().Should().BeEmpty();
	}

	[Fact]
	public async Task AssignRolesAsync_NullRolesList_ReturnsImmediateSuccess()
	{
		// Arrange
		var managementClient = Substitute.For<IManagementApiClient>();
		var sut = CreateSut(managementApiClient: managementClient);

		// Act
		var result = await sut.AssignRolesAsync("auth0|user1", null!, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		managementClient.ReceivedCalls().Should().BeEmpty();
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
		// Arrange — no Management API call expected
		var managementClient = Substitute.For<IManagementApiClient>();
		var sut = CreateSut(managementApiClient: managementClient);

		// Act
		var result = await sut.RemoveRolesAsync("auth0|user1", [], CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
		managementClient.ReceivedCalls().Should().BeEmpty();
	}

	[Fact]
	public async Task ListUsersAsync_UserWithoutUserId_SkipsRoleLookupAndReturnsUser()
	{
		// Arrange
		var managementClient = Substitute.For<IManagementApiClient>();
		var usersClient = Substitute.For<IUsersClient>();
		var rolesClient = Substitute.For<Auth0.ManagementApi.Users.IRolesClient>();

		managementClient.Users.Returns(usersClient);
		usersClient.Roles.Returns(rolesClient);
		usersClient.ListAsync(
				Arg.Any<ListUsersRequestParameters>(),
				Arg.Any<RequestOptions?>(),
				Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<Pager<UserResponseSchema>>(
				new TestPager<UserResponseSchema>(
				[
					new UserResponseSchema
					{
						UserId = " ",
						Email = "missing-id@test.com",
						Name = "Missing Id"
					}
				])));

		var sut = CreateSut(managementApiClient: managementClient);

		// Act
		var result = await sut.ListUsersAsync(1, 10, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().ContainSingle();
		result.Value![0].UserId.Should().BeEmpty();
		result.Value[0].Email.Should().Be("missing-id@test.com");
		result.Value[0].Roles.Should().BeEmpty();
		rolesClient.ReceivedCalls().Should().BeEmpty();
	}

	private sealed class TestPager<TItem>(IReadOnlyList<TItem> items) : Pager<TItem>
	{
		public Page<TItem> CurrentPage { get; } = new(items);
		public bool HasNextPage => false;

		public Task<Page<TItem>> GetNextPageAsync(CancellationToken cancellationToken = default)
			=> Task.FromResult(Page<TItem>.Empty);

		public async IAsyncEnumerable<Page<TItem>> AsPagesAsync(
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			yield return CurrentPage;
			await Task.CompletedTask;
		}

		public async IAsyncEnumerator<TItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			foreach (var item in items)
			{
				yield return item;
			}

			await Task.CompletedTask;
		}
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

	// TODO: Test ListUsersAsync success path (returns populated list).
	// TODO: Test AssignRolesAsync success path (roles assigned, returns true).
	// TODO: Test RemoveRolesAsync success path (roles removed, returns true).
	// TODO: Test Auth0 API error → ResultErrorCode.ExternalService for all methods.
	// These can now be implemented with a fully injectable IManagementApiClient via NSubstitute.
}
