// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RemoveRoleCommand.cs
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
///   Command to remove a role from a user, executed by an administrator.
/// </summary>
public record RemoveRoleCommand(
	string AdminUserId,
	string AdminUserName,
	string TargetUserId,
	string RoleName) : IRequest<Result<bool>>;

/// <summary>
///   Handler for <see cref="RemoveRoleCommand" />.
/// </summary>
public sealed class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand, Result<bool>>
{
	private readonly IUserManagementService _userManagementService;
	private readonly IAuditLogRepository _auditLogRepository;
	private readonly IMediator _mediator;
	private readonly ILogger<RemoveRoleCommandHandler> _logger;

	public RemoveRoleCommandHandler(
		IUserManagementService userManagementService,
		IAuditLogRepository auditLogRepository,
		IMediator mediator,
		ILogger<RemoveRoleCommandHandler> logger)
	{
		_userManagementService = userManagementService;
		_auditLogRepository = auditLogRepository;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<Result<bool>> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation(
			"Admin {AdminUserId} removing role '{RoleName}' from user {TargetUserId}",
			request.AdminUserId,
			request.RoleName,
			request.TargetUserId);

		var result = await _userManagementService.RemoveRolesAsync(
			request.TargetUserId,
			[request.RoleName],
			cancellationToken);

		if (result.Failure)
		{
			_logger.LogError(
				"Failed to remove role '{RoleName}' from user {TargetUserId}: {Error}",
				request.RoleName,
				request.TargetUserId,
				result.Error);

			return Result.Fail<bool>(
				result.Error ?? "Failed to remove role",
				result.ErrorCode);
		}

		var auditEntry = new RoleChangeAuditEntry
		{
			Id = ObjectId.GenerateNewId(),
			AdminUserId = request.AdminUserId,
			AdminUserName = request.AdminUserName,
			TargetUserId = request.TargetUserId,
			TargetUserEmail = string.Empty,
			Action = "removed",
			RoleName = request.RoleName,
			Timestamp = DateTimeOffset.UtcNow
		};

		await _auditLogRepository.AddAsync(auditEntry, cancellationToken);

		await _mediator.Publish(new RoleRemovedEvent
		{
			AdminUserId = request.AdminUserId,
			TargetUserId = request.TargetUserId,
			RoleName = request.RoleName,
			Timestamp = DateTimeOffset.UtcNow
		}, cancellationToken);

		_logger.LogInformation(
			"Successfully removed role '{RoleName}' from user {TargetUserId}",
			request.RoleName,
			request.TargetUserId);

		return Result.Ok(true);
	}
}
