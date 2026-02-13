// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     UserService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================


// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     UserService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Interfaces.Services;

namespace Shared.Features.User;

/// <summary>
///   UserService class
/// </summary>
public class UserService(IUserRepository repository) : IUserService
{

	/// <summary>
	///   ArchiveUser method
	/// </summary>
	/// <param name="user">UserModel</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task ArchiveUser(UserModel user)
	{
		ArgumentNullException.ThrowIfNull(user);

		return repository.ArchiveAsync(user);
	}

	/// <summary>
	///   CreateUser method
	/// </summary>
	/// <param name="user">UserModel</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task CreateUser(UserModel user)
	{
		ArgumentNullException.ThrowIfNull(user);

		return repository.CreateAsync(user);
	}

	/// <summary>
	///   GetUser method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of UserModel</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<UserModel> GetUser(string? userId)
	{
		ArgumentException.ThrowIfNullOrEmpty(userId);

		UserModel results = await repository.GetAsync(userId);

		return results;
	}

	/// <summary>
	///   GetUsers method
	/// </summary>
	/// <returns>Task if List UserModel</returns>
	public async Task<List<UserModel>> GetUsers()
	{
		IEnumerable<UserModel> results = await repository.GetAllAsync();

		return results.ToList();
	}

	/// <summary>
	///   GetUserFromAuthentication method
	/// </summary>
	/// <param name="userObjectIdentifierId">string</param>
	/// <returns>Task of UserModel</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<UserModel> GetUserFromAuthentication(string? userObjectIdentifierId)
	{
		ArgumentException.ThrowIfNullOrEmpty(userObjectIdentifierId);

		UserModel results = await repository.GetFromAuthenticationAsync(userObjectIdentifierId);

		return results;
	}

	/// <summary>
	///   UpdateUser method
	/// </summary>
	/// <param name="user">UserModel</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task UpdateUser(UserModel user)
	{
		ArgumentNullException.ThrowIfNull(user);

		return repository.UpdateAsync(user.Id, user);
	}
}