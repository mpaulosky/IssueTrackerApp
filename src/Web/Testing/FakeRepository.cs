// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     FakeRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using System.Linq.Expressions;
using System.Reflection;

using Domain.Abstractions;

namespace Web.Testing;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IRepository{TEntity}"/> used in the
/// Testing environment to provide fast, self-contained E2E test data without a real database.
/// </summary>
internal sealed class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
	private readonly List<TEntity> _store;
	private readonly Lock _lock = new();

	internal FakeRepository(IEnumerable<TEntity>? seed = null)
	{
		_store = seed?.ToList() ?? [];
	}

	private static PropertyInfo? IdProperty =>
		typeof(TEntity).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

	/// <inheritdoc/>
	public Task<Result<TEntity>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var entity = _store.FirstOrDefault(e => IdProperty?.GetValue(e)?.ToString() == id);
			return Task.FromResult(entity is not null
				? Result.Ok(entity)
				: Result.Fail<TEntity>($"Entity with id '{id}' not found.", ResultErrorCode.NotFound));
		}
	}

	/// <inheritdoc/>
	public Task<Result<IEnumerable<TEntity>>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			return Task.FromResult(Result.Ok<IEnumerable<TEntity>>(_store.ToList()));
		}
	}

	/// <inheritdoc/>
	public Task<Result<IEnumerable<TEntity>>> FindAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var compiled = predicate.Compile();
			var results = _store.Where(compiled).ToList();
			return Task.FromResult(Result.Ok<IEnumerable<TEntity>>(results));
		}
	}

	/// <inheritdoc/>
	public Task<Result<TEntity?>> FirstOrDefaultAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var compiled = predicate.Compile();
			var entity = _store.FirstOrDefault(compiled);
			return Task.FromResult(Result.Ok<TEntity?>(entity));
		}
	}

	/// <inheritdoc/>
	public Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			_store.Add(entity);
			return Task.FromResult(Result.Ok(entity));
		}
	}

	/// <inheritdoc/>
	public Task<Result<IEnumerable<TEntity>>> AddRangeAsync(
		IEnumerable<TEntity> entities,
		CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var list = entities.ToList();
			_store.AddRange(list);
			return Task.FromResult(Result.Ok<IEnumerable<TEntity>>(list));
		}
	}

	/// <inheritdoc/>
	public Task<Result<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var id = IdProperty?.GetValue(entity)?.ToString();
			var idx = _store.FindIndex(e => IdProperty?.GetValue(e)?.ToString() == id);
			if (idx >= 0)
				_store[idx] = entity;
			return Task.FromResult(Result.Ok(entity));
		}
	}

	/// <inheritdoc/>
	public Task<Result<bool>> DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var entity = _store.FirstOrDefault(e => IdProperty?.GetValue(e)?.ToString() == id);
			if (entity is null)
				return Task.FromResult(Result.Ok(false));
			_store.Remove(entity);
			return Task.FromResult(Result.Ok(true));
		}
	}

	/// <inheritdoc/>
	public Task<Result<bool>> AnyAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var compiled = predicate.Compile();
			return Task.FromResult(Result.Ok(_store.Any(compiled)));
		}
	}

	/// <inheritdoc/>
	public Task<Result<int>> CountAsync(
		Expression<Func<TEntity, bool>>? predicate = null,
		CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var count = predicate is null
				? _store.Count
				: _store.Count(predicate.Compile());
			return Task.FromResult(Result.Ok(count));
		}
	}
}
