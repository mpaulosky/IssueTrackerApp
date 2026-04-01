// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuditLogWriterService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Persistence.MongoDb
// =============================================

using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

using Microsoft.Extensions.Logging;

namespace Persistence.MongoDb.Services;

/// <summary>
///   MongoDB implementation of <see cref="IAuditLogWriterService" />.
///   Provides append-only audit log writing for role change events.
/// </summary>
public sealed class AuditLogWriterService : IAuditLogWriterService
{
	private readonly IIssueTrackerDbContext _context;
	private readonly ILogger<AuditLogWriterService> _logger;

	public AuditLogWriterService(
		IIssueTrackerDbContext context,
		ILogger<AuditLogWriterService> logger)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc />
	public async Task AddAsync(RoleChangeAuditEntry entry, CancellationToken ct)
	{
		await _context.Set<RoleChangeAuditEntry>().AddAsync(entry, ct);
		await _context.SaveChangesAsync(ct);

		_logger.LogInformation(
			"Audit entry recorded: action='{Action}' role='{RoleName}' targetUser='{TargetUserId}' by actor='{AdminUserId}'",
			entry.Action,
			entry.RoleName,
			entry.TargetUserId,
			entry.AdminUserId);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<RoleChangeAuditEntry>> GetByTargetUserAsync(
		string targetUserId,
		CancellationToken ct)
	{
		var entries = await _context
			.Set<RoleChangeAuditEntry>()
			.Where(e => e.TargetUserId == targetUserId)
			.ToListAsync(ct);

		return entries.AsReadOnly();
	}
}
