// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

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
	///   Updates an existing comment.
	/// </summary>
	Task<Result<CommentDto>> UpdateCommentAsync(
		string commentId,
		string title,
		string description,
		string requestingUserId,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Deletes (archives) a comment.
	/// </summary>
	Task<Result<bool>> DeleteCommentAsync(
		string commentId,
		string requestingUserId,
		bool isAdmin,
		UserDto archivedBy,
		CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of ICommentService using MediatR.
/// </summary>
public sealed class CommentService : ICommentService
{
	private readonly IMediator _mediator;
	private readonly Domain.Abstractions.INotificationService _notificationService;

	public CommentService(IMediator mediator, Domain.Abstractions.INotificationService notificationService)
	{
		_mediator = mediator;
		_notificationService = notificationService;
	}

	public async Task<Result<IReadOnlyList<CommentDto>>> GetCommentsAsync(
		string issueId,
		bool includeArchived = false,
		CancellationToken cancellationToken = default)
	{
		var query = new GetIssueCommentsQuery(issueId, includeArchived);
		return await _mediator.Send(query, cancellationToken);
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

		// Notify clients if successful
		if (result.Success && result.Value is not null)
		{
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
		string title,
		string description,
		string requestingUserId,
		CancellationToken cancellationToken = default)
	{
		var command = new UpdateCommentCommand(commentId, title, description, requestingUserId);
		return await _mediator.Send(command, cancellationToken);
	}

	public async Task<Result<bool>> DeleteCommentAsync(
		string commentId,
		string requestingUserId,
		bool isAdmin,
		UserDto archivedBy,
		CancellationToken cancellationToken = default)
	{
		var command = new DeleteCommentCommand(commentId, requestingUserId, isAdmin, archivedBy);
		return await _mediator.Send(command, cancellationToken);
	}
}
