// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
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
///   Implementation of IStatusService using MediatR.
/// </summary>
public sealed class StatusService : IStatusService
{
	private readonly IMediator _mediator;

	public StatusService(IMediator mediator)
	{
		_mediator = mediator;
	}

	public async Task<Result<IEnumerable<StatusDto>>> GetStatusesAsync(
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var query = new GetStatusesQuery(includeArchived);
		return await _mediator.Send(query, cancellationToken);
	}

	public async Task<Result<StatusDto>> GetStatusByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		var query = new GetStatusByIdQuery(id);
		return await _mediator.Send(query, cancellationToken);
	}

	public async Task<Result<StatusDto>> CreateStatusAsync(
		string statusName,
		string statusDescription,
		CancellationToken cancellationToken = default)
	{
		var command = new CreateStatusCommand(statusName, statusDescription);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<StatusDto>> UpdateStatusAsync(
		string id,
		string statusName,
		string statusDescription,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateStatusCommand(id, statusName, statusDescription);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<StatusDto>> ArchiveStatusAsync(
		string id,
		bool archive,
		UserDto archivedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new ArchiveStatusCommand(id, archive, archivedBy);
		return await _mediator.Send(command, cancellationToken);
	}
}
