// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueTrackerDbContext.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using Domain.Models;
using Microsoft.Extensions.Options;
using Persistence.MongoDb.Configurations;

namespace Persistence.MongoDb;

/// <summary>
///   Database context for IssueTracker application using MongoDB.
/// </summary>
public sealed class IssueTrackerDbContext : DbContext
{
	private readonly MongoDbSettings _settings;

	public IssueTrackerDbContext(
		DbContextOptions<IssueTrackerDbContext> options,
		IOptions<MongoDbSettings> settings) : base(options)
	{
		_settings = settings.Value;
	}

	/// <summary>
	///   Gets or sets the Issues collection.
	/// </summary>
	public DbSet<Issue> Issues => Set<Issue>();

	/// <summary>
	///   Gets or sets the Categories collection.
	/// </summary>
	public DbSet<Category> Categories => Set<Category>();

	/// <summary>
	///   Gets or sets the Statuses collection.
	/// </summary>
	public DbSet<Status> Statuses => Set<Status>();

	/// <summary>
	///   Gets or sets the Comments collection.
	/// </summary>
	public DbSet<Comment> Comments => Set<Comment>();

	/// <summary>
	///   Gets or sets the Attachments collection.
	/// </summary>
	public DbSet<Attachment> Attachments => Set<Attachment>();

	// Note: Users are not stored in MongoDB - they come from Auth0

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			optionsBuilder.UseMongoDB(
				_settings.ConnectionString,
				_settings.DatabaseName);
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Apply all configurations from the assembly
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(IssueTrackerDbContext).Assembly);

		// Configure entity keys (MongoDB uses string IDs or ObjectId)
		modelBuilder.Entity<Issue>().HasKey(e => e.Id);
		modelBuilder.Entity<Category>().HasKey(e => e.Id);
		modelBuilder.Entity<Status>().HasKey(e => e.Id);
		modelBuilder.Entity<Comment>().HasKey(e => e.Id);
		modelBuilder.Entity<Attachment>().HasKey(e => e.Id);
		// Note: User entity is not persisted - comes from Auth0
	}

	/// <summary>
	///   Ensures the database and collections are created with proper indexes.
	/// </summary>
	public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			// Ensure database is created
			await Database.EnsureCreatedAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Failed to initialize database.", ex);
		}
	}
}
