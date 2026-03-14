// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteCommentCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Mappers;

namespace Domain.Features.Comments.Commands;

/// <summary>
///   Command to delete (archive) a comment.
/// </summary>
public record DeleteCommentCommand(
	string CommentId,
	string RequestingUserId,
	bool IsAdmin,
	UserDto ArchivedBy) : IRequest<Result<bool>>;

/// <summary>
///   Handler for deleting (archiving) a comment.
/// </summary>
public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Result<bool>>
{
	private readonly IRepository<Comment> _repository;
	private readonly ILogger<DeleteCommentCommandHandler> _logger;

	public DeleteCommentCommandHandler(
		IRepository<Comment> repository,
		ILogger<DeleteCommentCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<bool>> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting comment with ID: {CommentId}", request.CommentId);

		var result = await _repository.GetByIdAsync(request.CommentId, cancellationToken);

		if (result.Failure || result.Value is null)
		{
			_logger.LogWarning("Comment not found with ID: {CommentId}", request.CommentId);
			return Result.Fail<bool>("Comment not found", ResultErrorCode.NotFound);
		}

		var comment = result.Value;

		// Check if the requesting user is the author or an admin
		var isOwner = comment.Author.Id == request.RequestingUserId;
		if (!isOwner && !request.IsAdmin)
		{
			_logger.LogWarning("User {UserId} attempted to delete comment {CommentId} without permission",
				request.RequestingUserId, request.CommentId);
			return Result.Fail<bool>("Only the comment author or an admin can delete this comment", ResultErrorCode.Validation);
		}

		// Soft delete (archive) the comment
		comment.Archived = true;
		comment.ArchivedBy = UserMapper.ToInfo(request.ArchivedBy);
		comment.DateModified = DateTime.UtcNow;

		var updateResult = await _repository.UpdateAsync(comment, cancellationToken);

		if (updateResult.Failure)
		{
			_logger.LogError("Failed to delete comment: {Error}", updateResult.Error);
			return Result.Fail<bool>(updateResult.Error ?? "Failed to delete comment", updateResult.ErrorCode);
		}

		_logger.LogInformation("Successfully deleted comment with ID: {CommentId}", request.CommentId);
		return Result.Ok(true);
	}
}
