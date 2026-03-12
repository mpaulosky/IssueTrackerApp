// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Issues.Commands;
using Domain.Features.Issues.Queries;
using MediatR;

namespace Web.Services;

/// <summary>
///   Service facade for Issue CRUD operations, wrapping MediatR calls.
/// </summary>
public interface IIssueService
{
	/// <summary>
	///   Gets a paginated list of issues with optional filters.
	/// </summary>
	Task<Result<PaginatedResponse<IssueDto>>> GetIssuesAsync(
		int page = 1,
		int pageSize = 10,
		string? statusFilter = null,
		string? categoryFilter = null,
		bool includeArchived = false,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Gets a single issue by ID.
	/// </summary>
	Task<Result<IssueDto>> GetIssueByIdAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	///   Creates a new issue.
	/// </summary>
	Task<Result<IssueDto>> CreateIssueAsync(
		string title,
		string description,
		CategoryDto category,
		UserDto author,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Updates an existing issue.
	/// </summary>
	Task<Result<IssueDto>> UpdateIssueAsync(
		string id,
		string title,
		string description,
		CategoryDto category,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Soft deletes (archives) an issue.
	/// </summary>
	Task<Result<bool>> DeleteIssueAsync(
		string id,
		UserDto archivedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Changes the status of an issue.
	/// </summary>
	Task<Result<IssueDto>> ChangeIssueStatusAsync(
		string id,
		StatusDto newStatus,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Searches issues with text search, filters, and pagination.
	/// </summary>
	Task<Result<PagedResult<IssueDto>>> SearchIssuesAsync(
		IssueSearchRequest request,
		CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of IIssueService using MediatR.
/// </summary>
public sealed class IssueService : IIssueService
{
	private readonly IMediator _mediator;

	public IssueService(IMediator mediator)
	{
		_mediator = mediator;
	}

	public async Task<Result<PaginatedResponse<IssueDto>>> GetIssuesAsync(
		int page = 1,
		int pageSize = 10,
		string? statusFilter = null,
		string? categoryFilter = null,
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var query = new GetIssuesQuery(page, pageSize, statusFilter, categoryFilter, includeArchived);
		return await _mediator.Send(query, cancellationToken);
	}

	public async Task<Result<IssueDto>> GetIssueByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		var query = new GetIssueByIdQuery(id);
		return await _mediator.Send(query, cancellationToken);
	}

	public async Task<Result<IssueDto>> CreateIssueAsync(
		string title,
		string description,
		CategoryDto category,
		UserDto author,
		CancellationToken cancellationToken = default)
	{
		var command = new CreateIssueCommand(title, description, category, author);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<IssueDto>> UpdateIssueAsync(
		string id,
		string title,
		string description,
		CategoryDto category,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateIssueCommand(id, title, description, category);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<bool>> DeleteIssueAsync(
		string id,
		UserDto archivedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new DeleteIssueCommand(id, archivedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<IssueDto>> ChangeIssueStatusAsync(
		string id,
		StatusDto newStatus,
		CancellationToken cancellationToken = default)
	{
		var command = new ChangeIssueStatusCommand(id, newStatus);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<PagedResult<IssueDto>>> SearchIssuesAsync(
		IssueSearchRequest request,
		CancellationToken cancellationToken = default)
	{
		var query = new SearchIssuesQuery(request);
		return await _mediator.Send(query, cancellationToken);
	}
}
