// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleChangeAuditEntryConfiguration.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Persistence.MongoDb
// =============================================

using Domain.Features.Admin.Models;

using MongoDB.EntityFrameworkCore.Extensions;

namespace Persistence.MongoDb.Configurations;

/// <summary>
///   Entity Framework Core configuration for the <see cref="RoleChangeAuditEntry" /> entity.
/// </summary>
public class RoleChangeAuditEntryConfiguration : IEntityTypeConfiguration<RoleChangeAuditEntry>
{
	public void Configure(EntityTypeBuilder<RoleChangeAuditEntry> builder)
	{
		// Store in a dedicated MongoDB collection
		builder.ToCollection("role_change_audit_log");

		// Configure ObjectId primary key
		builder.HasKey(e => e.Id);
	}
}
