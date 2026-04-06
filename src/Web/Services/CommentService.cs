// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CommentService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Comments.Commands;
using Domain.Features.Comments.Queries;
using Domain.Features.Issues.Queries;

using MediatR;

namespace Web.Services;

/// <summary>
///   Service facade for Comment CRUD operations, wrapping MediatR calls.
/// </summary>
public interface ICommentService
{
	/// <summary>
	///   Gets all comments for a specific issue.
	/// </summary>
	Task<Result<IReadOnlyList<CommentDto>>> GetCommentsAsync(
		string issueId,
		bool includeArchived = false,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Adds a new comment to an issue.
	/// </summary>
	Task<Result<CommentDto>> AddCommentAsync(
		string issueId,
		string title,
		string description,
		UserDto author,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Updates an existing comment and invalidates the per-issue comment cache.
	/// </summary>
	Task<Result<CommentDto>> UpdateCommentAsync(
		string commentId,
		string issueId,
		string title,
		string description,
		string requestingUserId,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Deletes (archives) a comment and invalidates the per-issue comment cache.
	/// </summary>
	Task<Result<bool>> DeleteCommentAsync(
		string commentId,
		string issueId,
		string requestingUserId,
		bool isAdmin,
		UserDto archivedBy,
		CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of ICommentService using MediatR with cache-aside reads
///   (5-minute TTL) and write-through invalidation.
/// </summary>
public sealed class CommentService : ICommentService
{
	private const string CommentsByIssueKeyPrefix = "comments_issue_";
	private static readonly TimeSpan CommentCacheTtl = TimeSpan.FromMinutes(5);

	private readonly IMediator _mediator;
	private readonly Domain.Abstractions.INotificationService _notificationService;
	private readonly DistributedCacheHelper _cache;

	public CommentService(
		IMediator mediator,
		Domain.Abstractions.INotificationService notificationService,
		DistributedCacheHelper cache)
	{
		_mediator = mediator;
		_notificationService = notificationService;
		_cache = cache;
	}

	public async Task<Result<IReadOnlyList<CommentDto>>> GetCommentsAsync(
		string issueId,
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		// Only cache the default (non-archived) view to keep logic simple.
		// Archived views are admin-only and low-traffic; let them bypass the cache.
		if (!includeArchived)
		{
			var cacheKey = $"{CommentsByIssueKeyPrefix}{issueId}";
			var cached = await _cache.GetAsync<List<CommentDto>>(cacheKey, cancellationToken);
			if (cached is not null)
			{
				return Result.Ok<IReadOnlyList<CommentDto>>(cached);
			}

			var query = new GetIssueCommentsQuery(issueId, includeArchived);
			var result = await _mediator.Send(query, cancellationToken);

			if (result.Success && result.Value is not null)
			{
				await _cache.SetAsync(cacheKey, result.Value.ToList(), CommentCacheTtl, cancellationToken);
			}

			return result;
		}

		var archivedQuery = new GetIssueCommentsQuery(issueId, includeArchived);
		return await _mediator.Send(archivedQuery, cancellationToken);
	}

	public async Task<Result<CommentDto>> AddCommentAsync(
		string issueId,
		string title,
		string description,
		UserDto author,
		CancellationToken cancellationToken = default)
	{
		var command = new AddCommentCommand(issueId, title, description, author);
		var result = await _mediator.Send(command, cancellationToken);

		if (result.Success && result.Value is not null)
		{
			// Invalidate comment list cache before optional side-effects that might throw.
			await _cache.RemoveAsync($"{CommentsByIssueKeyPrefix}{issueId}", cancellationToken);

			var issueResult = await _mediator.Send(new GetIssueByIdQuery(issueId), cancellationToken);
			if (issueResult.Success && issueResult.Value is not null)
			{
				await _notificationService.NotifyCommentAddedAsync(
					issueResult.Value.Id,
					issueResult.Value.Title,
					issueResult.Value.Author.Id,
					result.Value,
					cancellationToken);
			}
		}

		return result;
	}

	public async Task<Result<CommentDto>> UpdateCommentAsync(
		string commentId,
		string issueId,
		string title,
		string description,
		string requestingUserId,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateCommentCommand(commentId, title, description, requestingUserId);
		var result = await _mediator.Send(command, cancellationToken);

		// Cache invalidation is best-effort: callers (e.g. REST clients) that do
		// not supply issueId receive TTL-only expiry (≤5 min).  The Blazor
		// component always provides issueId, so the UI is never stale.
		if (result.Success && !string.IsNullOrEmpty(issueId))
		{
			await _cache.RemoveAsync($"{CommentsByIssueKeyPrefix}{issueId}", cancellationToken);
		}

		return result;
	}

	public async Task<Result<bool>> DeleteCommentAsync(
		string commentId,
		string issueId,
		string requestingUserId,
		bool isAdmin,
		UserDto archivedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new DeleteCommentCommand(commentId, requestingUserId, isAdmin, archivedBy);
		var result = await _mediator.Send(command, cancellationToken);

		// Cache invalidation is best-effort: callers that omit issueId receive TTL-only
		// expiry (≤5 min).  The Blazor component always provides issueId.
		if (result.Success && !string.IsNullOrEmpty(issueId))
		{
			await _cache.RemoveAsync($"{CommentsByIssueKeyPrefix}{issueId}", cancellationToken);
		}

		return result;
	}
}
