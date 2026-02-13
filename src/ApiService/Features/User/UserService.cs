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
	/// <param name="user">User</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task ArchiveUser(Shared.Models.User user)
	{
		ArgumentNullException.ThrowIfNull(user);

		return repository.ArchiveAsync(user);
	}

	/// <summary>
	///   CreateUser method
	/// </summary>
	/// <param name="user">User</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task CreateUser(Shared.Models.User user)
	{
		ArgumentNullException.ThrowIfNull(user);

		return repository.CreateAsync(user);
	}

	/// <summary>
	///   GetUser method
	/// </summary>
	/// <param name="userId">string</param>
	/// <returns>Task of User</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<Shared.Models.User> GetUser(string? userId)
	{
		ArgumentException.ThrowIfNullOrEmpty(userId);

		Shared.Models.User results = await repository.GetAsync(userId);

		return results;
	}

	/// <summary>
	///   GetUsers method
	/// </summary>
	/// <returns>Task if List User</returns>
	public async Task<List<Shared.Models.User>> GetUsers()
	{
		IEnumerable<Shared.Models.User> results = await repository.GetAllAsync();

		return results.ToList();
	}

	/// <summary>
	///   GetUserFromAuthentication method
	/// </summary>
	/// <param name="userObjectIdentifierId">string</param>
	/// <returns>Task of User</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public async Task<Shared.Models.User> GetUserFromAuthentication(string? userObjectIdentifierId)
	{
		ArgumentException.ThrowIfNullOrEmpty(userObjectIdentifierId);

		Shared.Models.User results = await repository.GetFromAuthenticationAsync(userObjectIdentifierId);

		return results;
	}

	/// <summary>
	///   UpdateUser method
	/// </summary>
	/// <param name="user">User</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task UpdateUser(Shared.Models.User user)
	{
		ArgumentNullException.ThrowIfNull(user);

		return repository.UpdateAsync(user.Id, user);
	}
}
