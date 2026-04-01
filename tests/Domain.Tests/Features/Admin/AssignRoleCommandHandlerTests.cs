// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AssignRoleCommandHandlerTests.cs
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
///   Unit tests for <see cref="AssignRoleCommandHandler" />.
/// </summary>
public sealed class AssignRoleCommandHandlerTests
{
	private readonly IUserManagementService _userManagementService;
	private readonly IAuditLogRepository _auditLogRepository;
	private readonly IMediator _mediator;
	private readonly ILogger<AssignRoleCommandHandler> _logger;
	private readonly AssignRoleCommandHandler _sut;

	public AssignRoleCommandHandlerTests()
	{
		_userManagementService = Substitute.For<IUserManagementService>();
		_auditLogRepository = Substitute.For<IAuditLogRepository>();
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<AssignRoleCommandHandler>>();
		_sut = new AssignRoleCommandHandler(
			_userManagementService,
			_auditLogRepository,
			_mediator,
			_logger);
	}

	[Fact]
	public async Task Handle_SuccessPath_ReturnsTrueResult()
	{
		// Arrange
		var command = new AssignRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.AssignRolesAsync("user|1", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task Handle_SuccessPath_PersistsAuditEntryWithCorrectData()
	{
		// Arrange
		var command = new AssignRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.AssignRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
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
				e.Action == "assigned"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_SuccessPath_PublishesRoleAssignedEvent()
	{
		// Arrange
		var command = new AssignRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.AssignRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Publish(
			Arg.Is<RoleAssignedEvent>(e =>
				e.AdminUserId == "admin|1" &&
				e.TargetUserId == "user|1" &&
				e.RoleName == "Admin"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_Auth0ApiError_PropagatesExternalServiceErrorCode()
	{
		// Arrange
		var command = new AssignRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.AssignRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Auth0 Management API returned 503", ResultErrorCode.ExternalService));

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
		var command = new AssignRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.AssignRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Auth0 API error", ResultErrorCode.ExternalService));

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _auditLogRepository.DidNotReceive()
			.AddAsync(Arg.Any<RoleChangeAuditEntry>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_ServiceFailure_DoesNotPublishRoleAssignedEvent()
	{
		// Arrange
		var command = new AssignRoleCommand("admin|1", "Admin User", "user|1", "Admin");

		_userManagementService
			.AssignRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Auth0 API error", ResultErrorCode.ExternalService));

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _mediator.DidNotReceive()
			.Publish(Arg.Any<RoleAssignedEvent>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_ValidationFailure_PropagatesValidationErrorCode()
	{
		// Arrange - unknown role name causes validation error from service
		var command = new AssignRoleCommand("admin|1", "Admin User", "user|1", "NonExistentRole");

		_userManagementService
			.AssignRolesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Unknown role(s): NonExistentRole", ResultErrorCode.Validation));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	// TODO: The handler does not currently enforce caller authorization (AdminUserId validity).
	// Authorization is handled by ASP.NET Core policy at the endpoint layer (RequireAuthorization("AdminPolicy")).
	// If a dedicated Unauthorized check is needed inside the handler, add it and test here.
	// Expected: Result.Fail<bool>("...", ResultErrorCode.Unauthorized) if that code is added to the enum.
}
