// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     InMemoryUndoService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Features.Issues.Commands.Bulk;

using Microsoft.Extensions.Caching.Memory;

namespace Web.Services;

/// <summary>
///   In-memory implementation of IUndoService using IMemoryCache.
///   Undo data expires after 5 minutes.
/// </summary>
public sealed class InMemoryUndoService : IUndoService
{
	private readonly IMemoryCache _cache;
	private readonly ILogger<InMemoryUndoService> _logger;
	private const int UndoExpirationMinutes = 5;
	private const string CacheKeyPrefix = "undo_";

	public InMemoryUndoService(
		IMemoryCache cache,
		ILogger<InMemoryUndoService> logger)
	{
		_cache = cache;
		_logger = logger;
	}

	public Task<string> StoreUndoDataAsync(
		string requestedBy,
		List<IssueUndoSnapshot> snapshots,
		CancellationToken cancellationToken = default)
	{
		var token = Guid.NewGuid().ToString("N");
		var cacheKey = $"{CacheKeyPrefix}{token}";

		var undoData = new UndoData(requestedBy, snapshots, DateTime.UtcNow);

		var cacheOptions = new MemoryCacheEntryOptions()
			.SetAbsoluteExpiration(TimeSpan.FromMinutes(UndoExpirationMinutes))
			.SetPriority(CacheItemPriority.High);

		_cache.Set(cacheKey, undoData, cacheOptions);

		_logger.LogDebug(
			"Stored undo data for {Count} issues with token {Token}, expires in {Minutes} minutes",
			snapshots.Count,
			token,
			UndoExpirationMinutes);

		return Task.FromResult(token);
	}

	public Task<UndoData?> GetUndoDataAsync(
		string undoToken,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{CacheKeyPrefix}{undoToken}";

		if (!_cache.TryGetValue(cacheKey, out UndoData? undoData) || undoData is null)
		{
			_logger.LogDebug("Undo data not found for token {Token}", undoToken);
			return Task.FromResult<UndoData?>(null);
		}

		// Verify the requester matches the original user
		if (!string.Equals(undoData.RequestedBy, requestedBy, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning(
				"Undo token {Token} requested by {Requester} but belongs to {Owner}",
				undoToken,
				requestedBy,
				undoData.RequestedBy);
			return Task.FromResult<UndoData?>(null);
		}

		return Task.FromResult<UndoData?>(undoData);
	}

	public Task InvalidateUndoTokenAsync(
		string undoToken,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{CacheKeyPrefix}{undoToken}";
		_cache.Remove(cacheKey);

		_logger.LogDebug("Invalidated undo token {Token}", undoToken);

		return Task.CompletedTask;
	}
}
