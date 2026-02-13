// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IUserService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Services;

/// <summary>
///   Provides methods for managing users.
/// </summary>
public interface IUserService
{
	/// <summary>
	///   Archives the specified user.
	/// </summary>
	/// <param name="user">The user to archive.</param>
	/// <returns>A task representing the asynchronous archive operation.</returns>
	Task ArchiveUser(Shared.Models.User user);

	/// <summary>
	///   Creates a new user.
	/// </summary>
	/// <param name="user">The user to create.</param>
	/// <returns>A task representing the asynchronous create operation.</returns>
	Task CreateUser(Shared.Models.User user);

	/// <summary>
	///   Gets a user by its identifier.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the user.</returns>
	Task<Shared.Models.User> GetUser(string? userId);

	/// <summary>
	///   Gets a user by its authentication object identifier.
	/// </summary>
	/// <param name="userObjectIdentifierId">The user authentication object identifier.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the user.</returns>
	Task<Shared.Models.User> GetUserFromAuthentication(string? userObjectIdentifierId);

	/// <summary>
	///   Gets all users.
	/// </summary>
	/// <returns>A task representing the asynchronous operation. The task result contains a list of users.</returns>
	Task<List<Shared.Models.User>> GetUsers();

	/// <summary>
	///   Updates the specified user.
	/// </summary>
	/// <param name="user">The user to update.</param>
	/// <returns>A task representing the asynchronous update operation.</returns>
	Task UpdateUser(Shared.Models.User user);
}
