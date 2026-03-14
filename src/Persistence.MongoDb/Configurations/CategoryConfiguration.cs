// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryConfiguration.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using Domain.Models;

namespace Persistence.MongoDb.Configurations;

/// <summary>
///   Entity Framework configuration for Category entity.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
	public void Configure(EntityTypeBuilder<Category> builder)
	{
		// Configure the key
		builder.HasKey(c => c.Id);

		// Configure owned type for embedded UserInfo
		builder.OwnsOne(c => c.ArchivedBy, ab =>
		{
			ab.Property(u => u.Id).HasElementName("id");
			ab.Property(u => u.Name).HasElementName("name");
			ab.Property(u => u.Email).HasElementName("email");
		});
	}
}
