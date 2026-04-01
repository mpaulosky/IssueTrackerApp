// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ListUsersQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;
using Domain.Features.Admin.Users.Queries;

namespace Domain.Tests.Features.Admin;

/// <summary>
///   Unit tests for <see cref="ListUsersQueryHandler" />.
/// </summary>
public sealed class ListUsersQueryHandlerTests
{
	private readonly IUserManagementService _userManagementService;
	private readonly ILogger<ListUsersQueryHandler> _logger;
	private readonly ListUsersQueryHandler _sut;

	public ListUsersQueryHandlerTests()
	{
		_userManagementService = Substitute.For<IUserManagementService>();
		_logger = Substitute.For<ILogger<ListUsersQueryHandler>>();
		_sut = new ListUsersQueryHandler(_userManagementService, _logger);
	}

	[Fact]
	public async Task Handle_WithUsers_ReturnsSuccessWithUserList()
	{
		// Arrange
		var users = new List<AdminUserSummary>
		{
			new() { UserId = "auth0|1", Email = "alice@example.com", Name = "Alice Smith" },
			new() { UserId = "auth0|2", Email = "bob@example.com", Name = "Bob Jones" }
		};
		var query = new ListUsersQuery(1, 10, null);

		_userManagementService
			.ListUsersAsync(1, 10, Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<AdminUserSummary>>(users));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value![0].Email.Should().Be("alice@example.com");
	}

	[Fact]
	public async Task Handle_EmptyUserList_ReturnsSuccessWithEmptyList()
	{
		// Arrange
		var query = new ListUsersQuery(1, 10, null);

		_userManagementService
			.ListUsersAsync(1, 10, Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<AdminUserSummary>>(new List<AdminUserSummary>()));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task Handle_PaginationParams_ForwardedToService()
	{
		// Arrange
		var query = new ListUsersQuery(3, 25, null);

		_userManagementService
			.ListUsersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<AdminUserSummary>>(new List<AdminUserSummary>()));

		// Act
		await _sut.Handle(query, CancellationToken.None);

		// Assert
		await _userManagementService.Received(1)
			.ListUsersAsync(3, 25, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WithSearchTerm_FiltersMatchingUsersOnly()
	{
		// Arrange
		var users = new List<AdminUserSummary>
		{
			new() { UserId = "auth0|1", Email = "alice@example.com", Name = "Alice Smith" },
			new() { UserId = "auth0|2", Email = "bob@example.com", Name = "Bob Jones" }
		};
		var query = new ListUsersQuery(1, 10, "alice");

		_userManagementService
			.ListUsersAsync(1, 10, Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<AdminUserSummary>>(users));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(1);
		result.Value![0].Name.Should().Be("Alice Smith");
	}

	[Fact]
	public async Task Handle_SearchTermMatchesEmail_ReturnsMatchingUser()
	{
		// Arrange
		var users = new List<AdminUserSummary>
		{
			new() { UserId = "auth0|1", Email = "alice@example.com", Name = "Alice Smith" },
			new() { UserId = "auth0|2", Email = "bob@example.com", Name = "Bob Jones" }
		};
		var query = new ListUsersQuery(1, 10, "bob@example.com");

		_userManagementService
			.ListUsersAsync(1, 10, Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<AdminUserSummary>>(users));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(1);
		result.Value![0].UserId.Should().Be("auth0|2");
	}

	[Fact]
	public async Task Handle_NullSearchTerm_ReturnsAllUsers()
	{
		// Arrange
		var users = new List<AdminUserSummary>
		{
			new() { UserId = "auth0|1", Email = "alice@example.com", Name = "Alice" },
			new() { UserId = "auth0|2", Email = "bob@example.com", Name = "Bob" }
		};
		var query = new ListUsersQuery(1, 10, null);

		_userManagementService
			.ListUsersAsync(1, 10, Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<AdminUserSummary>>(users));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task Handle_ServiceReturnsFailure_PropagatesErrorAndCode()
	{
		// Arrange
		var query = new ListUsersQuery(1, 10, null);

		_userManagementService
			.ListUsersAsync(1, 10, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IReadOnlyList<AdminUserSummary>>(
				"Auth0 connection failed",
				ResultErrorCode.ExternalService));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.ExternalService);
		result.Error.Should().Be("Auth0 connection failed");
	}
}
