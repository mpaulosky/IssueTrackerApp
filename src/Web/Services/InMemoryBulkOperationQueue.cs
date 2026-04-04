// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     InMemoryBulkOperationQueue.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using System.Collections.Concurrent;
using System.Threading.Channels;

using Domain.Features.Issues.Commands.Bulk;

using Microsoft.Extensions.Caching.Memory;

namespace Web.Services;

/// <summary>
///   In-memory implementation of IBulkOperationQueue using Channel for async processing.
/// </summary>
public sealed class InMemoryBulkOperationQueue : IBulkOperationQueue
{
	private readonly Channel<QueuedBulkOperation> _channel;
	private readonly IMemoryCache _cache;
	private readonly ILogger<InMemoryBulkOperationQueue> _logger;
	private readonly ConcurrentDictionary<string, BulkOperationResult> _results = new();
	private const string StatusCacheKeyPrefix = "bulk_status_";
	private const int StatusExpirationMinutes = 30;

	public InMemoryBulkOperationQueue(
		IMemoryCache cache,
		ILogger<InMemoryBulkOperationQueue> logger)
	{
		_cache = cache;
		_logger = logger;

		// Unbounded channel for queuing operations
		_channel = Channel.CreateUnbounded<QueuedBulkOperation>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false
		});
	}

	public async Task<string> QueueAsync<T>(T command, CancellationToken cancellationToken = default)
		where T : class
	{
		var operationId = Guid.NewGuid().ToString("N");

		var queuedOperation = new QueuedBulkOperation(
			operationId,
			command,
			typeof(T).Name,
			DateTime.UtcNow);

		// Store initial status before writing to channel to avoid race condition
		// where background service processes and sets terminal status before Queued is set
		await UpdateStatusAsync(operationId, BulkOperationStatus.Queued, null, cancellationToken);

		await _channel.Writer.WriteAsync(queuedOperation, cancellationToken);

		_logger.LogInformation(
			"Queued bulk operation {OperationId} of type {Type}",
			operationId,
			typeof(T).Name);

		return operationId;
	}

	public async Task<QueuedBulkOperation?> DequeueAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			return await _channel.Reader.ReadAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			return null;
		}
	}

	public Task<BulkOperationStatus?> GetStatusAsync(
		string operationId,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{StatusCacheKeyPrefix}{operationId}";

		if (_cache.TryGetValue(cacheKey, out BulkOperationStatus status))
		{
			return Task.FromResult<BulkOperationStatus?>(status);
		}

		return Task.FromResult<BulkOperationStatus?>(null);
	}

	public Task UpdateStatusAsync(
		string operationId,
		BulkOperationStatus status,
		BulkOperationResult? result = null,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{StatusCacheKeyPrefix}{operationId}";

		var cacheOptions = new MemoryCacheEntryOptions()
			.SetAbsoluteExpiration(TimeSpan.FromMinutes(StatusExpirationMinutes));

		_cache.Set(cacheKey, status, cacheOptions);

		if (result is not null)
		{
			_results[operationId] = result;
		}

		_logger.LogDebug(
			"Updated bulk operation {OperationId} status to {Status}",
			operationId,
			status);

		return Task.CompletedTask;
	}

	/// <summary>
	///   Gets the result of a completed operation.
	/// </summary>
	public BulkOperationResult? GetResult(string operationId)
	{
		_results.TryGetValue(operationId, out var result);
		return result;
	}

	/// <summary>
	///   Gets the channel reader for background processing.
	/// </summary>
	public ChannelReader<QueuedBulkOperation> Reader => _channel.Reader;
}
