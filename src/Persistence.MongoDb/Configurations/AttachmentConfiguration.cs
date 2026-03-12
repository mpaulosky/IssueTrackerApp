// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AttachmentConfiguration.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using Domain.Models;

namespace Persistence.MongoDb.Configurations;

/// <summary>
///   Entity Framework configuration for Attachment entity.
/// </summary>
public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
	public void Configure(EntityTypeBuilder<Attachment> builder)
	{
		// Configure the key
		builder.HasKey(a => a.Id);

		// Complex type for UploadedBy
		builder.OwnsOne(a => a.UploadedBy, ub =>
		{
			ub.Property(u => u.Id).HasElementName("id");
			ub.Property(u => u.Name).HasElementName("name");
			ub.Property(u => u.Email).HasElementName("email");
		});
	}
}
