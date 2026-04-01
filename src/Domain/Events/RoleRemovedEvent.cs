// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleRemovedEvent.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Events;

/// <summary>
///   Event raised when a role is removed from a user by an administrator.
/// </summary>
public sealed record RoleRemovedEvent : INotification
{
	/// <summary>Gets the Auth0 identifier of the administrator who performed the removal.</summary>
	public required string AdminUserId { get; init; }

	/// <summary>Gets the Auth0 identifier of the user whose role was removed.</summary>
	public required string TargetUserId { get; init; }

	/// <summary>Gets the name of the role that was removed.</summary>
	public required string RoleName { get; init; }

	/// <summary>Gets the UTC timestamp when the removal occurred.</summary>
	public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
