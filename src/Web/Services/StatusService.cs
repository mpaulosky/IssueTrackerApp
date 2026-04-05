// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     StatusService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Statuses.Commands;
using Domain.Features.Statuses.Queries;

using MediatR;

namespace Web.Services;

/// <summary>
///   Service facade for Status CRUD operations, wrapping MediatR calls.
/// </summary>
public interface IStatusService
{
	/// <summary>
	///   Gets all statuses with optional filtering.
	/// </summary>
	Task<Result<IEnumerable<StatusDto>>> GetStatusesAsync(
		bool includeArchived = false,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Gets a single status by ID.
	/// </summary>
	Task<Result<StatusDto>> GetStatusByIdAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	///   Creates a new status.
	/// </summary>
	Task<Result<StatusDto>> CreateStatusAsync(
		string statusName,
		string statusDescription,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Updates an existing status.
	/// </summary>
	Task<Result<StatusDto>> UpdateStatusAsync(
		string id,
		string statusName,
		string statusDescription,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Archives or unarchives a status.
	/// </summary>
	Task<Result<StatusDto>> ArchiveStatusAsync(
		string id,
		bool archive,
		UserDto archivedBy,
		CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of IStatusService using MediatR with cache-aside reads
///   (60-minute TTL) and write-through invalidation.
/// </summary>
public sealed class StatusService : IStatusService
{
	private const string CacheKeyList = "statuses_list";
	private const string CacheKeyPrefix = "status_";
	private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

	private readonly IMediator _mediator;
	private readonly DistributedCacheHelper _cacheHelper;

	public StatusService(IMediator mediator, DistributedCacheHelper cacheHelper)
	{
		_mediator = mediator;
		_cacheHelper = cacheHelper;
	}

	public async Task<Result<IEnumerable<StatusDto>>> GetStatusesAsync(
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{CacheKeyList}_{includeArchived}";

		var cached = await _cacheHelper.GetAsync<List<StatusDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok<IEnumerable<StatusDto>>(cached);
		}

		var query = new GetStatusesQuery(includeArchived);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await _cacheHelper.SetAsync(cacheKey, result.Value.ToList(), CacheTtl, cancellationToken);
		}

		return result;
	}

	public async Task<Result<StatusDto>> GetStatusByIdAsync(
		string id,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{CacheKeyPrefix}{id}";

		var cached = await _cacheHelper.GetAsync<StatusDto>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok(cached);
		}

		var query = new GetStatusByIdQuery(id);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await _cacheHelper.SetAsync(cacheKey, result.Value, CacheTtl, cancellationToken);
		}

		return result;
	}

	public async Task<Result<StatusDto>> CreateStatusAsync(
		string statusName,
		string statusDescription,
		CancellationToken cancellationToken = default)
	{
		var command = new CreateStatusCommand(statusName, statusDescription);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await InvalidateListCacheAsync(cancellationToken);
		}

		return result;
	}

	public async Task<Result<StatusDto>> UpdateStatusAsync(
		string id,
		string statusName,
		string statusDescription,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateStatusCommand(id, statusName, statusDescription);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cacheHelper.RemoveAsync($"{CacheKeyPrefix}{id}", cancellationToken);
			await InvalidateListCacheAsync(cancellationToken);
		}

		return result;
	}

	public async Task<Result<StatusDto>> ArchiveStatusAsync(
		string id,
		bool archive,
		UserDto archivedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new ArchiveStatusCommand(id, archive, archivedBy);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cacheHelper.RemoveAsync($"{CacheKeyPrefix}{id}", cancellationToken);
			await InvalidateListCacheAsync(cancellationToken);
		}

		return result;
	}

	private async Task InvalidateListCacheAsync(CancellationToken ct)
	{
		await _cacheHelper.RemoveAsync($"{CacheKeyList}_True", ct);
		await _cacheHelper.RemoveAsync($"{CacheKeyList}_False", ct);
	}
}
