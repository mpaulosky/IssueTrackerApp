// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusConfiguration.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using Domain.Models;

namespace Persistence.MongoDb.Configurations;

/// <summary>
///   Entity Framework configuration for Status entity.
/// </summary>
public class StatusConfiguration : IEntityTypeConfiguration<Status>
{
	public void Configure(EntityTypeBuilder<Status> builder)
	{
		// Configure the key
		builder.HasKey(s => s.Id);

		// Configure owned type for embedded UserDto
		builder.OwnsOne(s => s.ArchivedBy, ab =>
		{
			ab.Property(u => u.Id).HasElementName("id");
			ab.Property(u => u.Name).HasElementName("name");
			ab.Property(u => u.Email).HasElementName("email");
		});
	}
}
