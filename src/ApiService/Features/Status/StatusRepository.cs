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
public class StatusRepository(IMongoDbContextFactory context) : IStatusRepository
{
	private readonly IMongoCollection<StatusModel> _collection =
		context.GetCollection<StatusModel>(GetCollectionName(nameof(StatusModel)));

	/// <summary>
	///   ArchiveStatus method
	/// </summary>
	/// <param name="status">StatusModel</param>
	public async Task ArchiveAsync(StatusModel status)
	{
		// Archive the category
		status.Archived = true;

		await UpdateAsync(status.Id, status);
	}

	/// <summary>
	///   CreateStatus method
	/// </summary>
	/// <param name="status">StatusModel</param>
	public async Task CreateAsync(StatusModel status)
	{
		await _collection.InsertOneAsync(status);
	}

	/// <summary>
	///   GetStatus method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <returns>Task of StatusModel</returns>
	public async Task<StatusModel> GetAsync(string itemId)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<StatusModel>? filter = Builders<StatusModel>.Filter.Eq("_id", objectId);

		StatusModel? result = (await _collection.FindAsync(filter)).FirstOrDefault();

		return result;
	}

	/// <summary>
	///   GetStatuses method
	/// </summary>
	/// <returns>Task of IEnumerable StatusModel</returns>
	public async Task<IEnumerable<StatusModel>> GetAllAsync()
	{
		FilterDefinition<StatusModel>? filter = Builders<StatusModel>.Filter.Empty;

		List<StatusModel>? result = (await _collection.FindAsync(filter)).ToList();

		return result;
	}

	/// <summary>
	///   UpdateStatus method
	/// </summary>
	/// <param name="itemId">string</param>
	/// <param name="status">StatusModel</param>
	public async Task UpdateAsync(string itemId, StatusModel status)
	{
		ObjectId objectId = new(itemId);

		FilterDefinition<StatusModel>? filter = Builders<StatusModel>.Filter.Eq("_id", objectId);

		await _collection.ReplaceOneAsync(filter, status);
	}
}