// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IUserRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Repository;

/// <summary>
///   Provides repository methods for user entities.
/// </summary>
public interface IUserRepository
{
	/// <summary>
	///   Archives the specified user asynchronously.
	/// </summary>
	/// <param name="user">The user to archive.</param>
	/// <returns>A task representing the asynchronous archive operation.</returns>
	Task ArchiveAsync(Shared.Models.User user);

	/// <summary>
	///   Creates a new user asynchronously.
	/// </summary>
	/// <param name="user">The user to create.</param>
	/// <returns>A task representing the asynchronous create operation.</returns>
	Task CreateAsync(Shared.Models.User user);

	/// <summary>
	///   Gets a user by its identifier asynchronously.
	/// </summary>
	/// <param name="itemId">The user identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the user.</returns>
	Task<Shared.Models.User> GetAsync(string itemId);

	/// <summary>
	///   Gets a user by its authentication object identifier asynchronously.
	/// </summary>
	/// <param name="userObjectIdentifierId">The user authentication object identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the user.</returns>
	Task<Shared.Models.User> GetFromAuthenticationAsync(string userObjectIdentifierId);

	/// <summary>
	///   Gets all users asynchronously.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a collection of users.</returns>
	Task<IEnumerable<Shared.Models.User>> GetAllAsync();

	/// <summary>
	///   Updates the specified user asynchronously.
	/// </summary>
	/// <param name="itemId">The user identifier.</param>
	/// <param name="user">The user to update.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	Task UpdateAsync(string itemId, Shared.Models.User user);
}
