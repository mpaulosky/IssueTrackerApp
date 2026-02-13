// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IStatusRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Repository;

/// <summary>
///   Provides repository methods for status entities.
/// </summary>
public interface IStatusRepository
{
	/// <summary>
	///   Archives the specified status asynchronously.
	/// </summary>
	/// <param name="status">The status to archive.</param>
	/// <returns>A task representing the asynchronous archive operation.</returns>
	Task ArchiveAsync(Shared.Models.Status status);

	/// <summary>
	///   Creates a new status asynchronously.
	/// </summary>
	/// <param name="status">The status to create.</param>
	/// <returns>A task representing the asynchronous create operation.</returns>
	Task CreateAsync(Shared.Models.Status status);

	/// <summary>
	///   Gets a status by its identifier asynchronously.
	/// </summary>
	/// <param name="itemId">The status identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the status.</returns>
	Task<Shared.Models.Status> GetAsync(string itemId);

	/// <summary>
	///   Gets all statuses asynchronously.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a collection of statuses.</returns>
	Task<IEnumerable<Shared.Models.Status>> GetAllAsync();

	/// <summary>
	///   Updates the specified status asynchronously.
	/// </summary>
	/// <param name="itemId">The status identifier.</param>
	/// <param name="status">The status to update.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	Task UpdateAsync(string itemId, Shared.Models.Status status);
}
