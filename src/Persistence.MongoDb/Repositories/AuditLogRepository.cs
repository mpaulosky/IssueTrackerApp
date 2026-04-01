// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuditLogRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Persistence.MongoDb
// =============================================

using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

using Microsoft.Extensions.Logging;

namespace Persistence.MongoDb.Repositories;

/// <summary>
///   MongoDB implementation of <see cref="IAuditLogRepository" />.
/// </summary>
public sealed class AuditLogRepository : Repository<RoleChangeAuditEntry>, IRepository<RoleChangeAuditEntry>, IAuditLogRepository
{
	public AuditLogRepository(
		IIssueTrackerDbContext context,
		ILogger<Repository<RoleChangeAuditEntry>> logger)
		: base(context, logger)
	{
	}

	/// <inheritdoc />
	async Task IAuditLogRepository.AddAsync(RoleChangeAuditEntry entry, CancellationToken ct)
	{
		var result = await AddAsync(entry, ct);
		if (result.Failure)
			Logger.LogError("Failed to persist audit entry: {Error}", result.Error);
		else
			Logger.LogInformation(
				"Audit entry recorded: action='{Action}' role='{RoleName}' targetUser='{TargetUserId}' by admin='{AdminUserId}'",
				entry.Action, entry.RoleName, entry.TargetUserId, entry.AdminUserId);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<RoleChangeAuditEntry>> GetByTargetUserAsync(
		string targetUserId,
		CancellationToken ct)
	{
		var entries = await Context
			.Set<RoleChangeAuditEntry>()
			.Where(e => e.TargetUserId == targetUserId)
			.ToListAsync(ct);

		return entries.AsReadOnly();
	}
}
