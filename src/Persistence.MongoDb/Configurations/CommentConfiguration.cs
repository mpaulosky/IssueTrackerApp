// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentConfiguration.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using Domain.Models;

namespace Persistence.MongoDb.Configurations;

/// <summary>
///   Entity Framework configuration for Comment entity.
/// </summary>
public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
	public void Configure(EntityTypeBuilder<Comment> builder)
	{
		// Configure the key
		builder.HasKey(c => c.Id);

		// Configure owned type for Author
		builder.OwnsOne(c => c.Author, a =>
		{
			a.Property(u => u.Id).HasElementName("id");
			a.Property(u => u.Name).HasElementName("name");
			a.Property(u => u.Email).HasElementName("email");
		});

		// Configure owned type for ArchivedBy
		builder.OwnsOne(c => c.ArchivedBy, ab =>
		{
			ab.Property(u => u.Id).HasElementName("id");
			ab.Property(u => u.Name).HasElementName("name");
			ab.Property(u => u.Email).HasElementName("email");
		});

		// Configure owned type for AnswerSelectedBy
		builder.OwnsOne(c => c.AnswerSelectedBy, asb =>
		{
			asb.Property(u => u.Id).HasElementName("id");
			asb.Property(u => u.Name).HasElementName("name");
			asb.Property(u => u.Email).HasElementName("email");
		});

	}
}
