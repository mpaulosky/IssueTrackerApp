// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AssignRoleCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

using Domain.Abstractions;
using Domain.Events;
using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

namespace Domain.Features.Admin.Users.Commands;

/// <summary>
///   Command to assign a role to a user, executed by an administrator.
/// </summary>
public record AssignRoleCommand(
	string AdminUserId,
	string AdminUserName,
	string TargetUserId,
	string RoleName) : IRequest<Result<bool>>;

/// <summary>
///   Handler for <see cref="AssignRoleCommand" />.
/// </summary>
public sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<bool>>
{
	private readonly IUserManagementService _userManagementService;
	private readonly IAuditLogRepository _auditLogRepository;
	private readonly IMediator _mediator;
	private readonly ILogger<AssignRoleCommandHandler> _logger;

	public AssignRoleCommandHandler(
		IUserManagementService userManagementService,
		IAuditLogRepository auditLogRepository,
		IMediator mediator,
		ILogger<AssignRoleCommandHandler> logger)
	{
		_userManagementService = userManagementService;
		_auditLogRepository = auditLogRepository;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<Result<bool>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation(
			"Admin {AdminUserId} assigning role '{RoleName}' to user {TargetUserId}",
			request.AdminUserId,
			request.RoleName,
			request.TargetUserId);

		var result = await _userManagementService.AssignRolesAsync(
			request.TargetUserId,
			[request.RoleName],
			cancellationToken);

		if (result.Failure)
		{
			_logger.LogError(
				"Failed to assign role '{RoleName}' to user {TargetUserId}: {Error}",
				request.RoleName,
				request.TargetUserId,
				result.Error);

			return Result.Fail<bool>(
				result.Error ?? "Failed to assign role",
				result.ErrorCode);
		}

		var auditEntry = new RoleChangeAuditEntry
		{
			Id = ObjectId.GenerateNewId(),
			AdminUserId = request.AdminUserId,
			AdminUserName = request.AdminUserName,
			TargetUserId = request.TargetUserId,
			TargetUserEmail = string.Empty,
			Action = "assigned",
			RoleName = request.RoleName,
			Timestamp = DateTimeOffset.UtcNow
		};

		await _auditLogRepository.AddAsync(auditEntry, cancellationToken);

		await _mediator.Publish(new RoleAssignedEvent
		{
			AdminUserId = request.AdminUserId,
			TargetUserId = request.TargetUserId,
			RoleName = request.RoleName,
			Timestamp = DateTimeOffset.UtcNow
		}, cancellationToken);

		_logger.LogInformation(
			"Successfully assigned role '{RoleName}' to user {TargetUserId}",
			request.RoleName,
			request.TargetUserId);

		return Result.Ok(true);
	}
}
