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
		builder.ToCollection("attachments");

		// Configure the key
		builder.HasKey(a => a.Id);

		// Configure properties
		builder.Property(a => a.Id)
			.HasElementName("_id");

		builder.Property(a => a.IssueId)
			.HasElementName("issue_id")
			.IsRequired();

		builder.Property(a => a.FileName)
			.HasElementName("file_name")
			.IsRequired()
			.HasMaxLength(255);

		builder.Property(a => a.ContentType)
			.HasElementName("content_type")
			.IsRequired()
			.HasMaxLength(100);

		builder.Property(a => a.FileSize)
			.HasElementName("file_size")
			.IsRequired();

		builder.Property(a => a.BlobUrl)
			.HasElementName("blob_url")
			.IsRequired()
			.HasMaxLength(2048);

		builder.Property(a => a.ThumbnailUrl)
			.HasElementName("thumbnail_url")
			.HasMaxLength(2048);

		builder.Property(a => a.UploadedAt)
			.HasElementName("uploaded_at")
			.IsRequired();

		// Configure complex types
		builder.OwnsOne(a => a.UploadedBy, ub =>
		{
			ub.Property(u => u.Id).HasElementName("id");
			ub.Property(u => u.Name).HasElementName("name");
			ub.Property(u => u.Email).HasElementName("email");
		});
	}
}
