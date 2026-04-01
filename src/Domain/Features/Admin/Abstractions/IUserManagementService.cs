// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     IUserManagementService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

using Domain.Abstractions;
using Domain.Features.Admin.Models;

namespace Domain.Features.Admin.Abstractions;

/// <summary>
///   Service interface for managing Auth0 users and roles via the Management API.
/// </summary>
public interface IUserManagementService
{
	/// <summary>
	///   Returns a paginated list of users from Auth0.
	/// </summary>
	/// <param name="page">The 1-based page number.</param>
	/// <param name="perPage">The number of users per page (max 100).</param>
	/// <param name="ct">Cancellation token.</param>
	Task<Result<IReadOnlyList<AdminUserSummary>>> ListUsersAsync(
		int page,
		int perPage,
		CancellationToken ct);

	/// <summary>
	///   Returns a single user by their Auth0 user ID, including their assigned roles.
	/// </summary>
	/// <param name="userId">The Auth0 user identifier (e.g. <c>auth0|abc123</c>).</param>
	/// <param name="ct">Cancellation token.</param>
	Task<Result<AdminUserSummary>> GetUserByIdAsync(
		string userId,
		CancellationToken ct);

	/// <summary>
	///   Assigns one or more roles to a user, resolved by role name.
	/// </summary>
	/// <param name="userId">The Auth0 user identifier.</param>
	/// <param name="roleNames">Display names of the roles to assign (e.g. <c>Admin</c>, <c>User</c>).</param>
	/// <param name="ct">Cancellation token.</param>
	Task<Result<bool>> AssignRolesAsync(
		string userId,
		IEnumerable<string> roleNames,
		CancellationToken ct);

	/// <summary>
	///   Removes one or more roles from a user, resolved by role name.
	/// </summary>
	/// <param name="userId">The Auth0 user identifier.</param>
	/// <param name="roleNames">Display names of the roles to remove.</param>
	/// <param name="ct">Cancellation token.</param>
	Task<Result<bool>> RemoveRolesAsync(
		string userId,
		IEnumerable<string> roleNames,
		CancellationToken ct);

	/// <summary>
	///   Returns all roles defined in the Auth0 tenant.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	Task<Result<IReadOnlyList<RoleAssignment>>> ListRolesAsync(CancellationToken ct);
}
