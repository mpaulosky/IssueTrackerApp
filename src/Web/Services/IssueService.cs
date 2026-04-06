// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     IssueService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

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
		IReadOnlyList<string>? labels = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Updates an existing issue.
	/// </summary>
	Task<Result<IssueDto>> UpdateIssueAsync(
		string id,
		string title,
		string description,
		CategoryDto category,
		IReadOnlyList<string>? labels = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Soft deletes (archives) an issue.
	/// </summary>
	Task<Result<bool>> DeleteIssueAsync(
		string id,
		UserDto archivedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Restores (unarchives) a previously archived issue.
	/// </summary>
	Task<Result<bool>> RestoreIssueAsync(
		string id,
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
///   Implementation of IIssueService using MediatR with cache-aside reads
///   and version-token invalidation on all write operations.
/// </summary>
public sealed class IssueService : IIssueService
{
	private const string IssuesVersionKey   = "issues_version";
	private const string IssueByIdKeyPrefix = "issue_";
	private static readonly TimeSpan ListCacheTtl  = TimeSpan.FromMinutes(5);
	private static readonly TimeSpan ByIdCacheTtl  = TimeSpan.FromMinutes(10);

	private readonly IMediator _mediator;
	private readonly INotificationService _notificationService;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly DistributedCacheHelper _cache;

	public IssueService(
		IMediator mediator,
		INotificationService notificationService,
		IBulkOperationQueue bulkQueue,
		DistributedCacheHelper cache)
	{
		_mediator = mediator;
		_notificationService = notificationService;
		_bulkQueue = bulkQueue;
		_cache = cache;
	}

	public async Task<Result<PaginatedResponse<IssueDto>>> GetIssuesAsync(
		int page = 1,
		int pageSize = 10,
		string? statusFilter = null,
		string? categoryFilter = null,
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var version = await _cache.GetVersionAsync(IssuesVersionKey, cancellationToken);
		// Use | as segment separator to prevent collisions between filter values that contain _.
		var cacheKey = $"issues_list_{version}_{page}_{pageSize}|{statusFilter ?? ""}|{categoryFilter ?? ""}|{includeArchived}";

		var cached = await _cache.GetAsync<PaginatedResponse<IssueDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok(cached);
		}

		var query = new GetIssuesQuery(page, pageSize, statusFilter, categoryFilter, includeArchived);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await _cache.SetAsync(cacheKey, result.Value, ListCacheTtl, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IssueDto>> GetIssueByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{IssueByIdKeyPrefix}{id}";

		var cached = await _cache.GetAsync<IssueDto>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			return Result.Ok(cached);
		}

		var query = new GetIssueByIdQuery(id);
		var result = await _mediator.Send(query, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await _cache.SetAsync(cacheKey, result.Value, ByIdCacheTtl, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IssueDto>> CreateIssueAsync(
		string title,
		string description,
		CategoryDto category,
		UserDto author,
		IReadOnlyList<string>? labels = null,
		CancellationToken cancellationToken = default)
	{
		var command = new CreateIssueCommand(title, description, category, author);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			// Bump version immediately after the write succeeds — before optional side
			// effects (label attachment, notifications) that might throw.
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);

			// Attach labels one-by-one using the dedicated command.
			if (labels is { Count: > 0 })
			{
				var issueId = result.Value.Id.ToString();
				foreach (var label in labels)
				{
					if (!string.IsNullOrWhiteSpace(label))
					{
						await _mediator.Send(new AddLabelCommand(issueId, label), cancellationToken);
					}
				}

				// Re-fetch so the returned DTO includes the labels.
				var refreshed = await _mediator.Send(new GetIssueByIdQuery(issueId), cancellationToken);
				if (refreshed.Success && refreshed.Value is not null)
				{
					result = refreshed;
				}
			}

			await _notificationService.NotifyIssueCreatedAsync(result.Value!, cancellationToken);
		}

		return result;
	}

	public async Task<Result<IssueDto>> UpdateIssueAsync(
		string id,
		string title,
		string description,
		CategoryDto category,
		IReadOnlyList<string>? labels = null,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateIssueCommand(id, title, description, category);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			// Invalidate caches immediately after the write succeeds — before optional
			// side effects (label sync, notifications) that might throw.
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
			await _cache.RemoveAsync($"{IssueByIdKeyPrefix}{id}", cancellationToken);

			// Sync labels: add new ones, remove stale ones.
			if (labels is not null)
			{
				var currentLabels = result.Value.Labels ?? [];
				var desired = labels
					.Where(l => !string.IsNullOrWhiteSpace(l))
					.Select(l => l.Trim().ToLowerInvariant())
					.Distinct()
					.ToList();

				foreach (var toRemove in currentLabels.Except(desired, StringComparer.OrdinalIgnoreCase))
				{
					await _mediator.Send(new RemoveLabelCommand(id, toRemove), cancellationToken);
				}

				foreach (var toAdd in desired.Except(currentLabels, StringComparer.OrdinalIgnoreCase))
				{
					await _mediator.Send(new AddLabelCommand(id, toAdd), cancellationToken);
				}

				// Re-fetch so the returned DTO includes the updated labels.
				var refreshed = await _mediator.Send(new GetIssueByIdQuery(id), cancellationToken);
				if (refreshed.Success && refreshed.Value is not null)
				{
					result = refreshed;
				}
			}

			await _notificationService.NotifyIssueUpdatedAsync(result.Value!, cancellationToken);
		}

		return result;
	}

	public async Task<Result<bool>> DeleteIssueAsync(
		string id,
		UserDto archivedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new DeleteIssueCommand(id, archivedBy);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
			await _cache.RemoveAsync($"{IssueByIdKeyPrefix}{id}", cancellationToken);
		}

		return result;
	}

	public async Task<Result<bool>> RestoreIssueAsync(
		string id,
		CancellationToken cancellationToken = default)
	{
		var command = new RestoreIssueCommand(id);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
			await _cache.RemoveAsync($"{IssueByIdKeyPrefix}{id}", cancellationToken);
		}

		return result;
	}

	public async Task<Result<IssueDto>> ChangeIssueStatusAsync(
		string id,
		StatusDto newStatus,
		CancellationToken cancellationToken = default)
	{
		var command = new ChangeIssueStatusCommand(id, newStatus);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
			await _cache.RemoveAsync($"{IssueByIdKeyPrefix}{id}", cancellationToken);
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
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
		}

		return result;
	}

	public async Task<Result<BulkOperationResult>> BulkUpdateCategoryAsync(
		List<string> issueIds,
		CategoryDto newCategory,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new BulkUpdateCategoryCommand(issueIds, newCategory, requestedBy);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
		}

		return result;
	}

	public async Task<Result<BulkOperationResult>> BulkAssignAsync(
		List<string> issueIds,
		UserDto assignee,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new BulkAssignCommand(issueIds, assignee, requestedBy);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
		}

		return result;
	}

	public async Task<Result<BulkOperationResult>> BulkDeleteAsync(
		List<string> issueIds,
		UserDto deletedBy,
		string requestedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new BulkDeleteCommand(issueIds, deletedBy, requestedBy);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
		}

		return result;
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
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success)
		{
			await _cache.BumpVersionAsync(IssuesVersionKey, cancellationToken);
		}

		return result;
	}

	public async Task<BulkOperationStatus?> GetBulkOperationStatusAsync(
		string operationId,
		CancellationToken cancellationToken = default)
	{
		return await _bulkQueue.GetStatusAsync(operationId, cancellationToken);
	}
}
