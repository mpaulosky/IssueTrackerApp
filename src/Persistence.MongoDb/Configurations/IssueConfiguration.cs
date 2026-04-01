// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueConfiguration.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using Domain.Models;

namespace Persistence.MongoDb.Configurations;

/// <summary>
///   Entity Framework configuration for Issue entity.
/// </summary>
public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
	public void Configure(EntityTypeBuilder<Issue> builder)
	{
		// Configure the key
		builder.HasKey(i => i.Id);

		// Configure owned types for embedded DTOs
		builder.OwnsOne(i => i.Author, a =>
		{
			a.Property(u => u.Id).HasElementName("id");
			a.Property(u => u.Name).HasElementName("name");
			a.Property(u => u.Email).HasElementName("email");
		});

		builder.OwnsOne(i => i.ArchivedBy, ab =>
		{
			ab.Property(u => u.Id).HasElementName("id");
			ab.Property(u => u.Name).HasElementName("name");
			ab.Property(u => u.Email).HasElementName("email");
		});

		builder.OwnsOne(i => i.Assignee, a =>
		{
			a.Property(u => u.Id).HasElementName("id");
			a.Property(u => u.Name).HasElementName("name");
			a.Property(u => u.Email).HasElementName("email");
		});

		builder.OwnsOne(i => i.Category, c =>
		{
			c.Property(cat => cat.Id).HasElementName("id");
			c.Property(cat => cat.CategoryName).HasElementName("category_name");
			c.Property(cat => cat.CategoryDescription).HasElementName("category_description");
			c.Property(cat => cat.DateCreated).HasElementName("date_created");
			c.Property(cat => cat.DateModified).HasElementName("date_modified");
			c.Property(cat => cat.Archived).HasElementName("archived");
			// Ignore nested ArchivedBy within embedded CategoryInfo —
			// archival metadata belongs on the Category entity, not its embedded reference
			c.Ignore(cat => cat.ArchivedBy);
		});

		builder.OwnsOne(i => i.Status, s =>
		{
			s.Property(st => st.Id).HasElementName("id");
			s.Property(st => st.StatusName).HasElementName("status_name");
			s.Property(st => st.StatusDescription).HasElementName("status_description");
			s.Property(st => st.DateCreated).HasElementName("date_created");
			s.Property(st => st.DateModified).HasElementName("date_modified");
			s.Property(st => st.Archived).HasElementName("archived");
			// Ignore nested ArchivedBy within embedded StatusInfo —
			// archival metadata belongs on the Status entity, not its embedded reference
			s.Ignore(st => st.ArchivedBy);
		});

		builder.Property(i => i.Votes);
		builder.Property(i => i.VotedBy);
	}
}
