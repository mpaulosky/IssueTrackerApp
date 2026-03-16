// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IIssueTrackerDbContext.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using Domain.Models;

namespace Persistence.MongoDb;

/// <summary>
///   Interface for IssueTracker database context, enabling dependency injection and mocking.
/// </summary>
public interface IIssueTrackerDbContext
{
	/// <summary>
	///   Gets the Issues collection.
	/// </summary>
	DbSet<Issue> Issues { get; }

	/// <summary>
	///   Gets the Categories collection.
	/// </summary>
	DbSet<Category> Categories { get; }

	/// <summary>
	///   Gets the Statuses collection.
	/// </summary>
	DbSet<Status> Statuses { get; }

	/// <summary>
	///   Gets the Comments collection.
	/// </summary>
	DbSet<Comment> Comments { get; }

	/// <summary>
	///   Gets the Attachments collection.
	/// </summary>
	DbSet<Attachment> Attachments { get; }

	/// <summary>
	///   Gets the Email Queue collection.
	/// </summary>
	DbSet<EmailQueueItem> EmailQueue { get; }

	/// <summary>
	///   Gets a DbSet for the specified entity type.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <returns>A DbSet for the specified entity type.</returns>
	DbSet<TEntity> Set<TEntity>() where TEntity : class;

	/// <summary>
	///   Saves all changes made in this context to the database asynchronously.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>
	///   A task that represents the asynchronous save operation. The task result contains the
	///   number of state entries written to the database.
	/// </returns>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
