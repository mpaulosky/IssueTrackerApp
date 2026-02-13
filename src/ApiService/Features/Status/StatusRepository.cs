// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     StatusRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.PlugIns
// =============================================

namespace ApiService.Features.Status;

/// <summary>
///   StatusRepository class
/// </summary>
public class StatusRepository(IMongoDbContextFactory contextFactory) : IStatusRepository
{
	private readonly IMongoCollection<Shared.Models.Status> _collection = contextFactory.CreateDbContext().Statuses;

	/// <summary>
	///   ArchiveStatus method
	/// </summary>
	/// <param name="status">Status</param>
	public async Task ArchiveAsync(Shared.Models.Status status)
	{
		// Archive the category
		status.Archived = true;

		await UpdateAsync(status.Id, status);
	}

	/// <summary>
	///   CreateStatus method
	/// </summary>
	/// <param name="status">Status</param>
	public async Task CreateAsync(Shared.Models.Status status)
	{
		await _collection.InsertOneAsync(status);
	}

	/// <summary>
	///   GetStatus method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of Status</returns>
	public async Task<Shared.Models.Status> GetAsync(string itemId)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Status>? filter = Builders<Shared.Models.Status>.Filter.Eq("_id", objectId);

		Shared.Models.Status? result = (await _collection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   GetStatuses method
	/// </summary>
	/// <returns>Task of IEnumerable Status</returns>
	public async Task<IEnumerable<Shared.Models.Status>> GetAllAsync()
	{
		FilterDefinition<Shared.Models.Status>? filter = Builders<Shared.Models.Status>.Filter.Empty;

		List<Shared.Models.Status>? result = (await _collection.FindAsync(filter)).ToList();

		return result;
	}

	/// <summary>
	///   UpdateStatus method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="status">Status</param>
	public async Task UpdateAsync(string itemId, Shared.Models.Status status)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<Shared.Models.Status>? filter = Builders<Shared.Models.Status>.Filter.Eq("_id", objectId);

		await _collection.ReplaceOneAsync(filter, status);
	}
}
