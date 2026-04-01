// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AdminUserSummary.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Features.Admin.Models;

/// <summary>
///   Represents a summary of an administrative user sourced from Auth0.
/// </summary>
public record AdminUserSummary
{
	/// <summary>Gets the Auth0 user identifier.</summary>
	public string UserId { get; init; } = string.Empty;

	/// <summary>Gets the user email address.</summary>
	public string Email { get; init; } = string.Empty;

	/// <summary>Gets the user display name.</summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>Gets the URL of the user's profile picture.</summary>
	public string Picture { get; init; } = string.Empty;

	/// <summary>Gets the roles assigned to the user.</summary>
	public IReadOnlyList<string> Roles { get; init; } = [];

	/// <summary>Gets the date and time of the user's last login.</summary>
	public DateTimeOffset? LastLogin { get; init; }

	/// <summary>Gets a value indicating whether the user is blocked in Auth0.</summary>
	public bool IsBlocked { get; init; }

	/// <summary>Gets an empty <see cref="AdminUserSummary" /> instance.</summary>
	public static AdminUserSummary Empty => new();
}
