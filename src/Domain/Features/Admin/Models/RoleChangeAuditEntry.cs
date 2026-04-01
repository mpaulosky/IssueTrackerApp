// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleChangeAuditEntry.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Features.Admin.Models;

/// <summary>
///   Represents an audit log entry recording a role change performed by an administrator.
/// </summary>
public record RoleChangeAuditEntry
{
	/// <summary>Gets the unique identifier for this audit entry.</summary>
	public string Id { get; init; } = string.Empty;

	/// <summary>Gets the Auth0 user ID of the user whose roles were changed.</summary>
	public string TargetUserId { get; init; } = string.Empty;

	/// <summary>Gets the Auth0 user ID of the administrator who made the change.</summary>
	public string ActorUserId { get; init; } = string.Empty;

	/// <summary>Gets the action performed (e.g. <c>assigned</c> or <c>removed</c>).</summary>
	public string Action { get; init; } = string.Empty;

	/// <summary>Gets the names of the roles that were changed.</summary>
	public IReadOnlyList<string> RoleNames { get; init; } = [];

	/// <summary>Gets the UTC timestamp of the role change.</summary>
	public DateTimeOffset ChangedAt { get; init; }
}
