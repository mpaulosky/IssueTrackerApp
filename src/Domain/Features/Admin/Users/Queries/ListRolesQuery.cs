// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ListRolesQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

using Domain.Abstractions;
using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

namespace Domain.Features.Admin.Users.Queries;

/// <summary>
///   Query to retrieve all roles defined in the Auth0 tenant.
/// </summary>
public record ListRolesQuery : IRequest<Result<IReadOnlyList<RoleAssignment>>>;

/// <summary>
///   Handler for <see cref="ListRolesQuery" />.
/// </summary>
public sealed class ListRolesQueryHandler
	: IRequestHandler<ListRolesQuery, Result<IReadOnlyList<RoleAssignment>>>
{
	private readonly IUserManagementService _userManagementService;
	private readonly ILogger<ListRolesQueryHandler> _logger;

	public ListRolesQueryHandler(
		IUserManagementService userManagementService,
		ILogger<ListRolesQueryHandler> logger)
	{
		_userManagementService = userManagementService;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<RoleAssignment>>> Handle(
		ListRolesQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Listing all roles from Auth0");

		var result = await _userManagementService.ListRolesAsync(cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to list roles: {Error}", result.Error);
			return Result.Fail<IReadOnlyList<RoleAssignment>>(
				result.Error ?? "Failed to list roles",
				result.ErrorCode);
		}

		var roles = result.Value ?? (IReadOnlyList<RoleAssignment>)[];

		_logger.LogInformation("Successfully retrieved {Count} role(s)", roles.Count);
		return Result.Ok(roles);
	}
}
