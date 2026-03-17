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
using Domain.Features.Issues.Commands.Bulk;
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

	/// <summary>
	///   Bulk updates the status of multiple issues.
	/// </summary>
	Task<Result<BulkOperationResult>> BulkUpdateStatusAsync(
		List<string> issueIds,
		StatusDto newStatus,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Bulk updates the category of multiple issues.
	/// </summary>
	Task<Result<BulkOperationResult>> BulkUpdateCategoryAsync(
		List<string> issueIds,
		CategoryDto newCategory,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Bulk assigns multiple issues to a user.
	/// </summary>
	Task<Result<BulkOperationResult>> BulkAssignAsync(
		List<string> issueIds,
		UserDto assignee,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Bulk deletes (archives) multiple issues.
	/// </summary>
	Task<Result<BulkOperationResult>> BulkDeleteAsync(
		List<string> issueIds,
		UserDto deletedBy,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Bulk exports multiple issues to CSV.
	/// </summary>
	Task<Result<BulkExportResult>> BulkExportAsync(
		List<string> issueIds,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Undoes a previous bulk operation.
	/// </summary>
	Task<Result<BulkOperationResult>> UndoLastBulkOperationAsync(
		string undoToken,
		string requestedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Gets the status of a background bulk operation.
	/// </summary>
	Task<BulkOperationStatus?> GetBulkOperationStatusAsync(
		string operationId,
		CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of IIssueService using MediatR.
/// </summary>
public sealed class IssueService : IIssueService
{
	private readonly IMediator _mediator;
	private readonly Domain.Abstractions.INotificationService _notificationService;
	private readonly IBulkOperationQueue _bulkQueue;

	public IssueService(
		IMediator mediator,
		Domain.Abstractions.INotificationService notificationService,
		IBulkOperationQueue bulkQueue)
	{
		_mediator = mediator;
		_notificationService = notificationService;
		_bulkQueue = bulkQueue;
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
		var result = await _mediator.Send(command, cancellationToken);

		// Notify clients if successful
		if (result.Success && result.Value is not null)
		{
			await _notificationService.NotifyIssueCreatedAsync(result.Value, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IssueDto>> UpdateIssueAsync(
		string id,
		string title,
		string description,
		CategoryDto category,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateIssueCommand(id, title, description, category);
		var result = await _mediator.Send(command, cancellationToken);

		// Notify clients if successful
		if (result.Success && result.Value is not null)
		{
			await _notificationService.NotifyIssueUpdatedAsync(result.Value, cancellationToken);
		}

		return result;
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
		var result = await _mediator.Send(command, cancellationToken);

		// Notify clients if successful
		if (result.Success && result.Value is not null)
		{
			await _notificationService.NotifyIssueUpdatedAsync(result.Value, cancellationToken);
		}

		return result;
	}

	public async Task<Result<PagedResult<IssueDto>>> SearchIssuesAsync(
		IssueSearchRequest request,
		CancellationToken cancellationToken = default)
	{
		var query = new SearchIssuesQuery(request);
		return await _mediator.Send(query, cancellationToken);
	}

	public async Task<Result<BulkOperationResult>> BulkUpdateStatusAsync(
		List<string> issueIds,
		StatusDto newStatus,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new BulkUpdateStatusCommand(issueIds, newStatus, requestedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<BulkOperationResult>> BulkUpdateCategoryAsync(
		List<string> issueIds,
		CategoryDto newCategory,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new BulkUpdateCategoryCommand(issueIds, newCategory, requestedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<BulkOperationResult>> BulkAssignAsync(
		List<string> issueIds,
		UserDto assignee,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new BulkAssignCommand(issueIds, assignee, requestedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<BulkOperationResult>> BulkDeleteAsync(
		List<string> issueIds,
		UserDto deletedBy,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new BulkDeleteCommand(issueIds, deletedBy, requestedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<BulkExportResult>> BulkExportAsync(
		List<string> issueIds,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new BulkExportCommand(issueIds, requestedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<BulkOperationResult>> UndoLastBulkOperationAsync(
		string undoToken,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new UndoBulkOperationCommand(undoToken, requestedBy);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<BulkOperationStatus?> GetBulkOperationStatusAsync(
		string operationId,
		CancellationToken cancellationToken = default)
	{
		return await _bulkQueue.GetStatusAsync(operationId, cancellationToken);
	}
}
