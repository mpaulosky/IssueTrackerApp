// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Repository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

namespace Persistence.MongoDb.Repositories;

/// <summary>
///   Base repository implementation using MongoDB with Entity Framework Core.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
	protected readonly IssueTrackerDbContext Context;
	protected readonly DbSet<TEntity> DbSet;
	protected readonly ILogger<Repository<TEntity>> Logger;

	public Repository(
		IssueTrackerDbContext context,
		ILogger<Repository<TEntity>> logger)
	{
		Context = context ?? throw new ArgumentNullException(nameof(context));
		DbSet = context.Set<TEntity>();
		Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public virtual async Task<Result<TEntity>> GetByIdAsync(
		string id,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var entity = await DbSet.FindAsync([id], cancellationToken);

			return entity is null
				? Result.Fail<TEntity>($"{typeof(TEntity).Name} with ID '{id}' was not found.", ResultErrorCode.NotFound)
				: Result.Ok(entity);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error getting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
			return Result.Fail<TEntity>($"Failed to retrieve {typeof(TEntity).Name}: {ex.Message}");
		}
	}

	public virtual async Task<Result<IEnumerable<TEntity>>> GetAllAsync(
		CancellationToken cancellationToken = default)
	{
		try
		{
			var entities = await DbSet.ToListAsync(cancellationToken);
			return Result.Ok<IEnumerable<TEntity>>(entities);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error getting all {EntityType}", typeof(TEntity).Name);
			return Result.Fail<IEnumerable<TEntity>>(
				$"Failed to retrieve {typeof(TEntity).Name} entities: {ex.Message}");
		}
	}

	public virtual async Task<Result<IEnumerable<TEntity>>> FindAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var entities = await DbSet.Where(predicate).ToListAsync(cancellationToken);
			return Result.Ok<IEnumerable<TEntity>>(entities);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error finding {EntityType} with predicate", typeof(TEntity).Name);
			return Result.Fail<IEnumerable<TEntity>>(
				$"Failed to find {typeof(TEntity).Name} entities: {ex.Message}");
		}
	}

	public virtual async Task<Result<TEntity?>> FirstOrDefaultAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var entity = await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
			return Result.Ok(entity);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error getting first {EntityType} with predicate", typeof(TEntity).Name);
			return Result.Fail<TEntity?>(
				$"Failed to retrieve {typeof(TEntity).Name}: {ex.Message}");
		}
	}

	public virtual async Task<Result<TEntity>> AddAsync(
		TEntity entity,
		CancellationToken cancellationToken = default)
	{
		try
		{
			await DbSet.AddAsync(entity, cancellationToken);
			await Context.SaveChangesAsync(cancellationToken);

			Logger.LogInformation("Added {EntityType} entity", typeof(TEntity).Name);
			return Result.Ok(entity);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error adding {EntityType}", typeof(TEntity).Name);
			return Result.Fail<TEntity>(
				$"Failed to add {typeof(TEntity).Name}: {ex.Message}");
		}
	}

	public virtual async Task<Result<IEnumerable<TEntity>>> AddRangeAsync(
		IEnumerable<TEntity> entities,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var entityList = entities.ToList();
			await DbSet.AddRangeAsync(entityList, cancellationToken);
			await Context.SaveChangesAsync(cancellationToken);

			Logger.LogInformation("Added {Count} {EntityType} entities", entityList.Count, typeof(TEntity).Name);
			return Result.Ok<IEnumerable<TEntity>>(entityList);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error adding multiple {EntityType}", typeof(TEntity).Name);
			return Result.Fail<IEnumerable<TEntity>>(
				$"Failed to add {typeof(TEntity).Name} entities: {ex.Message}");
		}
	}

	public virtual async Task<Result<TEntity>> UpdateAsync(
		TEntity entity,
		CancellationToken cancellationToken = default)
	{
		try
		{
			DbSet.Update(entity);
			await Context.SaveChangesAsync(cancellationToken);

			Logger.LogInformation("Updated {EntityType} entity", typeof(TEntity).Name);
			return Result.Ok(entity);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error updating {EntityType}", typeof(TEntity).Name);
			return Result.Fail<TEntity>(
				$"Failed to update {typeof(TEntity).Name}: {ex.Message}");
		}
	}

	public virtual async Task<Result<bool>> DeleteAsync(
		string id,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var entity = await DbSet.FindAsync([id], cancellationToken);

			if (entity is null)
			{
				return Result.Fail<bool>($"{typeof(TEntity).Name} with ID '{id}' was not found.", ResultErrorCode.NotFound);
			}

			DbSet.Remove(entity);
			await Context.SaveChangesAsync(cancellationToken);

			Logger.LogInformation("Deleted {EntityType} with ID {Id}", typeof(TEntity).Name, id);
			return Result.Ok(true);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error deleting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
			return Result.Fail<bool>(
				$"Failed to delete {typeof(TEntity).Name}: {ex.Message}");
		}
	}

	public virtual async Task<Result<bool>> AnyAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var exists = await DbSet.AnyAsync(predicate, cancellationToken);
			return Result.Ok(exists);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error checking existence of {EntityType}", typeof(TEntity).Name);
			return Result.Fail<bool>(
				$"Failed to check {typeof(TEntity).Name} existence: {ex.Message}");
		}
	}

	public virtual async Task<Result<int>> CountAsync(
		Expression<Func<TEntity, bool>>? predicate = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var count = predicate is null
				? await DbSet.CountAsync(cancellationToken)
				: await DbSet.CountAsync(predicate, cancellationToken);

			return Result.Ok(count);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error counting {EntityType}", typeof(TEntity).Name);
			return Result.Fail<int>(
				$"Failed to count {typeof(TEntity).Name} entities: {ex.Message}");
		}
	}
}
