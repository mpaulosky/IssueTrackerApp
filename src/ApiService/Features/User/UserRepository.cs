// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     UserRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.PlugIns
// =============================================

namespace ApiService.Features.User;

/// <summary>
///   UserRepository class
/// </summary>
public class UserRepository(IMongoDbContextFactory contextFactory) : IUserRepository
{
	private readonly IMongoCollection<Shared.Models.User> _collection = contextFactory.CreateDbContext().Users;

	/// <summary>
	///   Archive User method
	/// </summary>
	/// <param name="user">User</param>
	/// <returns>Task</returns>
	public async Task ArchiveAsync(Shared.Models.User user)
	{
		// TODO: User model doesn't have Archived property yet
		// user.Archived = true;

		await UpdateAsync(user.Id, user);
	}

	/// <summary>
	///   CreateUser method
	/// </summary>
	/// <param name="user">User</param>
	public async Task CreateAsync(Shared.Models.User user)
	{
		await _collection.InsertOneAsync(user);
	}

	/// <summary>
	///   GetUser method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of User</returns>
	public async Task<Shared.Models.User> GetAsync(string itemId)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.User>? filter = Builders<Shared.Models.User>.Filter.Eq("_id", objectId);

		Shared.Models.User? result = (await _collection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   GetUsers method
	/// </summary>
	/// <returns>Task of IEnumerable User</returns>
	public async Task<IEnumerable<Shared.Models.User>> GetAllAsync()
	{
		FilterDefinition<Shared.Models.User>? filter = Builders<Shared.Models.User>.Filter.Empty;

		List<Shared.Models.User>? result = (await _collection.FindAsync(filter)).ToList();

		return result;
	}

	/// <summary>
	///   UpdateUser method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="user">User</param>
	public async Task UpdateAsync(string itemId, Shared.Models.User user)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.User>? filter = Builders<Shared.Models.User>.Filter.Eq("_id", objectId);

		await _collection.ReplaceOneAsync(filter!, user);
	}

	/// <summary>
	///   GetUserFromAuthentication method
	/// </summary>
	/// <param name="userObjectIdentifierId">string</param>
	/// <returns>Task of User</returns>
	public async Task<Shared.Models.User> GetFromAuthenticationAsync(string userObjectIdentifierId)
	{
		FilterDefinition<Shared.Models.User>? filter = Builders<Shared.Models.User>.Filter.Eq("object_identifier", userObjectIdentifierId);

		Shared.Models.User? result = (await _collection.FindAsync(filter)).FirstOrDefault();

		return result;
	}
}
