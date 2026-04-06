// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     DistributedCacheHelper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;

using Web.Helpers;

namespace Web.Services;

/// <summary>
///   Shared helper that wraps <see cref="IDistributedCache" /> with JSON
///   serialisation, structured logging, and a simple version-counter API.
/// </summary>
public sealed class DistributedCacheHelper
{
	private readonly IDistributedCache _cache;
	private readonly ILogger<DistributedCacheHelper> _logger;

	/// <summary>
	///   Serializer options that include the <see cref="ObjectIdJsonConverter" />
	///   so that DTOs containing MongoDB ObjectIds round-trip correctly.
	/// </summary>
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		Converters = { new ObjectIdJsonConverter() }
	};

	public DistributedCacheHelper(IDistributedCache cache, ILogger<DistributedCacheHelper> logger)
	{
		_cache = cache;
		_logger = logger;
	}

	/// <summary>
	///   Deserialises <typeparamref name="T" /> from the cache.
	///   Returns <c>null</c> on a cache miss or on a deserialisation error.
	/// </summary>
	public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
	{
		try
		{
			var bytes = await _cache.GetAsync(key, ct);
			if (bytes is null)
			{
				return default;
			}

			return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to deserialise cache entry for key '{Key}'", key);
			return default;
		}
	}

	/// <summary>
	///   Serialises <paramref name="value" /> and stores it with the given TTL.
	/// </summary>
	public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
	{
		try
		{
			var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
			var options = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = ttl
			};
			await _cache.SetAsync(key, bytes, options, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to set cache entry for key '{Key}'", key);
		}
	}

	/// <summary>
	///   Removes a single cache key.
	/// </summary>
	public async Task RemoveAsync(string key, CancellationToken ct = default)
	{
		try
		{
			await _cache.RemoveAsync(key, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to remove cache entry for key '{Key}'", key);
		}
	}

	/// <summary>
	///   Reads a version counter stored at <paramref name="key" />.
	///   Returns <c>0</c> if the key is not present.
	/// </summary>
	public async Task<long> GetVersionAsync(string key, CancellationToken ct = default)
	{
		try
		{
			var bytes = await _cache.GetAsync(key, ct);
			if (bytes is null)
			{
				return 0L;
			}

			var text = Encoding.UTF8.GetString(bytes);
			return long.TryParse(text, out var version) ? version : 0L;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to read version counter for key '{Key}'", key);
			return 0L;
		}
	}

	/// <summary>
	///   Increments the version counter stored at <paramref name="key" /> and
	///   returns the new value.  Uses a 24-hour TTL so counters self-expire.
	///   <para>
	///     Note: this is a best-effort increment over <see cref="IDistributedCache" />.
	///     True atomicity (e.g. Redis INCR) is not available through the cache
	///     abstraction.  Concurrent bumps may lose increments, which is acceptable
	///     for cache-version invalidation use cases.
	///   </para>
	/// </summary>
	public async Task<long> BumpVersionAsync(string key, CancellationToken ct = default)
	{
		try
		{
			var current = await GetVersionAsync(key, ct);
			var next = current + 1;
			var bytes = Encoding.UTF8.GetBytes(next.ToString());
			var options = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
			};
			await _cache.SetAsync(key, bytes, options, ct);
			return next;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to bump version counter for key '{Key}'", key);
			return 0L;
		}
	}
}
