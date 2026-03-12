using System.Linq.Expressions;

namespace Domain.Abstractions;

/// <summary>
/// Generic repository interface for data access operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
	/// <summary>
	/// Gets an entity by its ID.
	/// </summary>
	Task<Result<TEntity>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all entities.
	/// </summary>
	Task<Result<IEnumerable<TEntity>>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds entities matching a predicate.
	/// </summary>
	Task<Result<IEnumerable<TEntity>>> FindAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the first entity matching a predicate, or null if not found.
	/// </summary>
	Task<Result<TEntity?>> FirstOrDefaultAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds a new entity.
	/// </summary>
	Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds multiple entities.
	/// </summary>
	Task<Result<IEnumerable<TEntity>>> AddRangeAsync(
		IEnumerable<TEntity> entities,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing entity.
	/// </summary>
	Task<Result<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an entity by its ID.
	/// </summary>
	Task<Result<bool>> DeleteAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if any entity matches the predicate.
	/// </summary>
	Task<Result<bool>> AnyAsync(
		Expression<Func<TEntity, bool>> predicate,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Counts entities matching the predicate.
	/// </summary>
	Task<Result<int>> CountAsync(
		Expression<Func<TEntity, bool>>? predicate = null,
		CancellationToken cancellationToken = default);
}
