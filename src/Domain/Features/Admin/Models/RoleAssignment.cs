// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleAssignment.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Features.Admin.Models;

/// <summary>
///   Represents a role available for assignment in Auth0.
/// </summary>
public record RoleAssignment
{
	/// <summary>Gets the Auth0 role identifier.</summary>
	public string RoleId { get; init; } = string.Empty;

	/// <summary>Gets the role name.</summary>
	public string RoleName { get; init; } = string.Empty;

	/// <summary>Gets the role description.</summary>
	public string Description { get; init; } = string.Empty;
}
