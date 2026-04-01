// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RemoveRoleCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;
using Domain.Features.Admin.Users.Commands;

namespace Domain.Tests.Features.Admin;

/// <summary>
///   Unit tests for <see cref="RemoveRoleCommandHandler" />.
/// </summary>
public sealed class RemoveRoleCommandHandlerTests
{
	private readonly IUserManagementService _userManagementService;
	private readonly IAuditLogRepository _auditLogRepository;
	private readonly IMediator _mediator;
	private readonly ILogger<RemoveRoleCommandHandler> _logger;
	private readonly RemoveRoleCommandHandler _sut;

	public RemoveRoleCommandHandlerTests()
	{
		_userManagementService = Substitute.For<IUserManagementService>();
		_auditLogRepository = Substitute.For<IAuditLogRepository>();
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<RemoveRoleCommandHandler>>();
		_sut = new RemoveRoleCommandHandler(
			_userManagementService,
			_auditLogRepository,
			_mediator,
			_logger);
	}

	[Fact]
	public async Task Handle_SuccessPath_ReturnsTrueResult()
	{
		// Arrange
		var command = new RemoveRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.RemoveRolesAsync("user|1", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task Handle_SuccessPath_PersistsAuditEntryWithRemovedAction()
	{
		// Arrange
		var command = new RemoveRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.RemoveRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _auditLogRepository.Received(1).AddAsync(
			Arg.Is<RoleChangeAuditEntry>(e =>
				e.AdminUserId == "admin|1" &&
				e.AdminUserName == "Admin User" &&
				e.TargetUserId == "user|1" &&
				e.RoleName == "Admin" &&
				e.Action == "removed"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_SuccessPath_PublishesRoleRemovedEvent()
	{
		// Arrange
		var command = new RemoveRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.RemoveRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Publish(
			Arg.Is<RoleRemovedEvent>(e =>
				e.AdminUserId == "admin|1" &&
				e.TargetUserId == "user|1" &&
				e.RoleName == "Admin"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_UserNotFound_PropagatesExternalServiceError()
	{
		// Arrange - Auth0 returns 404 for unknown user; service maps this to ExternalService
		var command = new RemoveRoleCommand("admin|1", "Admin User", "unknown-user", "Admin");

		_userManagementService
			.RemoveRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Failed to remove roles: Not Found", ResultErrorCode.ExternalService));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.ExternalService);
	}

	[Fact]
	public async Task Handle_Auth0ApiError_PropagatesExternalServiceErrorCode()
	{
		// Arrange
		var command = new RemoveRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.RemoveRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Auth0 service unavailable", ResultErrorCode.ExternalService));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.ExternalService);
	}

	[Fact]
	public async Task Handle_ServiceFailure_DoesNotPersistAuditEntry()
	{
		// Arrange
		var command = new RemoveRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.RemoveRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Auth0 API error", ResultErrorCode.ExternalService));

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _auditLogRepository.DidNotReceive()
			.AddAsync(Arg.Any<RoleChangeAuditEntry>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_ServiceFailure_DoesNotPublishRoleRemovedEvent()
	{
		// Arrange
		var command = new RemoveRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.RemoveRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Auth0 API error", ResultErrorCode.ExternalService));

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _mediator.DidNotReceive()
			.Publish(Arg.Any<RoleRemovedEvent>(), Arg.Any<CancellationToken>());
	}

	// TODO: The handler does not currently enforce caller authorization (AdminUserId validity).
	// Authorization is handled by ASP.NET Core policy at the endpoint layer (RequireAuthorization("AdminPolicy")).
	// If a dedicated Unauthorized check is needed inside the handler, add it and test here.
}
