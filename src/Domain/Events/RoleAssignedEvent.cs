// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleAssignedEvent.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Events;

/// <summary>
///   Event raised when a role is assigned to a user by an administrator.
/// </summary>
public sealed record RoleAssignedEvent : INotification
{
	/// <summary>Gets the Auth0 identifier of the administrator who performed the assignment.</summary>
	public required string AdminUserId { get; init; }

	/// <summary>Gets the Auth0 identifier of the user who received the role.</summary>
	public required string TargetUserId { get; init; }

	/// <summary>Gets the name of the role that was assigned.</summary>
	public required string RoleName { get; init; }

	/// <summary>Gets the UTC timestamp when the assignment occurred.</summary>
	public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
