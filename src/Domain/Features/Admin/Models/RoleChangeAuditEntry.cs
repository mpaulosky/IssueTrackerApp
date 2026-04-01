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
///   Audit log record capturing every role assignment or removal performed by an admin.
/// </summary>
[Serializable]
public class RoleChangeAuditEntry
{
	/// <summary>
	///   Parameterless constructor required by the MongoDB driver for deserialization.
	/// </summary>
	public RoleChangeAuditEntry()
	{
	}

	/// <summary>Gets or sets the MongoDB document identifier.</summary>
	[BsonId]
	public ObjectId Id { get; set; } = ObjectId.Empty;

	/// <summary>Gets or sets the Auth0 identifier of the admin who performed the action.</summary>
	public string AdminUserId { get; set; } = string.Empty;

	/// <summary>Gets or sets the display name of the admin who performed the action.</summary>
	public string AdminUserName { get; set; } = string.Empty;

	/// <summary>Gets or sets the Auth0 identifier of the user whose role was changed.</summary>
	public string TargetUserId { get; set; } = string.Empty;

	/// <summary>Gets or sets the email address of the user whose role was changed.</summary>
	public string TargetUserEmail { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the action performed. Valid values are <c>"assigned"</c> or <c>"removed"</c>.
	/// </summary>
	public string Action { get; set; } = string.Empty;

	/// <summary>Gets or sets the name of the role that was assigned or removed.</summary>
	public string RoleName { get; set; } = string.Empty;

	/// <summary>Gets or sets the UTC timestamp when the action occurred.</summary>
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
