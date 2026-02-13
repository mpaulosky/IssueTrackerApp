// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     StatusService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================


// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     StatusService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Interfaces.Services;

namespace Shared.Features.Status;

/// <summary>
///   StatusService class
/// </summary>
public class StatusService(IStatusRepository repository, IMemoryCache cache) : IStatusService
{
	private const string CacheName = "StatusData";

	/// <summary>
	///   ArchiveStatus method
	/// </summary>
	/// <param name="status">Status</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task ArchiveStatus(Shared.Models.Status status)
	{
		ArgumentNullException.ThrowIfNull(status);

		cache.Remove(CacheName);

		return repository.ArchiveAsync(status);
	}

	/// <summary>
	///   CreateStatus method
	/// </summary>
	/// <param name="status">Status</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task CreateStatus(Shared.Models.Status status)
	{
		ArgumentNullException.ThrowIfNull(status);

		return repository.CreateAsync(status);
	}

	/// <summary>
	///   GetStatus method
	/// </summary>
	/// <param name="statusId">string</param>
	/// <returns>Task Status</returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentException"></exception>
	public async Task<Shared.Models.Status> GetStatus(string statusId)
	{
		ArgumentException.ThrowIfNullOrEmpty(statusId);

		Shared.Models.Status result = await repository.GetAsync(statusId);

		return result;
	}

	/// <summary>
	///   GetStatuses method
	/// </summary>
	/// <returns>Task of List Status</returns>
	public async Task<List<Shared.Models.Status>> GetStatuses()
	{
		List<Shared.Models.Status>? output = cache.Get<List<Shared.Models.Status>>(CacheName);

		if (output is not null)
		{
			return output;
		}

		IEnumerable<Shared.Models.Status> results = await repository.GetAllAsync();

		output = results.ToList();

		cache.Set(CacheName, output, TimeSpan.FromDays(1));

		return output;
	}

	/// <summary>
	///   UpdateStatus method
	/// </summary>
	/// <param name="status">Status</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task UpdateStatus(Shared.Models.Status status)
	{
		ArgumentNullException.ThrowIfNull(status);

		return repository.UpdateAsync(status.Id, status);
	}

	/// <summary>
	///   DeleteStatus method
	/// </summary>
	/// <param name="status">Status</param>
	/// <returns>Task</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public Task DeleteStatus(Shared.Models.Status status)
	{
		ArgumentNullException.ThrowIfNull(status);

		return repository.ArchiveAsync(status);
	}
}
