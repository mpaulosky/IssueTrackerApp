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

		// Configure owned type for embedded IssueDto
		builder.OwnsOne(c => c.Issue, i =>
		{
			i.Property(issue => issue.Id).HasElementName("id");
			i.Property(issue => issue.Title).HasElementName("title");
			i.Property(issue => issue.Description).HasElementName("description");
			i.Property(issue => issue.DateCreated).HasElementName("date_created");
			i.Property(issue => issue.DateModified).HasElementName("date_modified");
			i.Property(issue => issue.Archived).HasElementName("archived");
			i.Property(issue => issue.ApprovedForRelease).HasElementName("approved_for_release");
			i.Property(issue => issue.Rejected).HasElementName("rejected");

			// Nested owned types within IssueDto
			i.OwnsOne(issue => issue.Author, ia =>
			{
				ia.Property(u => u.Id).HasElementName("id");
				ia.Property(u => u.Name).HasElementName("name");
				ia.Property(u => u.Email).HasElementName("email");
			});

			i.OwnsOne(issue => issue.ArchivedBy, iab =>
			{
				iab.Property(u => u.Id).HasElementName("id");
				iab.Property(u => u.Name).HasElementName("name");
				iab.Property(u => u.Email).HasElementName("email");
			});

			i.OwnsOne(issue => issue.Category, ic =>
			{
				ic.Property(cat => cat.Id).HasElementName("id");
				ic.Property(cat => cat.CategoryName).HasElementName("category_name");
				ic.Property(cat => cat.CategoryDescription).HasElementName("category_description");
				ic.Property(cat => cat.DateCreated).HasElementName("date_created");
				ic.Property(cat => cat.DateModified).HasElementName("date_modified");
				ic.Property(cat => cat.Archived).HasElementName("archived");
				ic.OwnsOne(cat => cat.ArchivedBy, icab =>
				{
					icab.Property(u => u.Id).HasElementName("id");
					icab.Property(u => u.Name).HasElementName("name");
					icab.Property(u => u.Email).HasElementName("email");
				});
			});

			i.OwnsOne(issue => issue.Status, ist =>
			{
				ist.Property(st => st.Id).HasElementName("id");
				ist.Property(st => st.StatusName).HasElementName("status_name");
				ist.Property(st => st.StatusDescription).HasElementName("status_description");
				ist.Property(st => st.DateCreated).HasElementName("date_created");
				ist.Property(st => st.DateModified).HasElementName("date_modified");
				ist.Property(st => st.Archived).HasElementName("archived");
				ist.OwnsOne(st => st.ArchivedBy, istab =>
				{
					istab.Property(u => u.Id).HasElementName("id");
					istab.Property(u => u.Name).HasElementName("name");
					istab.Property(u => u.Email).HasElementName("email");
				});
			});
		});
	}
}
