// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     IAuditLogRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

using Domain.Features.Admin.Models;

namespace Domain.Features.Admin.Abstractions;

/// <summary>
///   Repository interface for persisting and querying <see cref="RoleChangeAuditEntry" /> records.
/// </summary>
public interface IAuditLogRepository
{
	/// <summary>
	///   Persists a new audit log entry.
	/// </summary>
	/// <param name="entry">The audit log entry to add.</param>
	/// <param name="ct">Cancellation token.</param>
	Task AddAsync(RoleChangeAuditEntry entry, CancellationToken ct);

	/// <summary>
	///   Returns all audit log entries for the specified target user.
	/// </summary>
	/// <param name="targetUserId">The Auth0 identifier of the target user.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A read-only list of matching audit log entries, ordered by insertion order.</returns>
	Task<IReadOnlyList<RoleChangeAuditEntry>> GetByTargetUserAsync(
		string targetUserId,
		CancellationToken ct);
}
