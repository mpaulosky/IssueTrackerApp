// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateCommentCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Comments.Commands;

/// <summary>
///   Command to update an existing comment.
/// </summary>
public record UpdateCommentCommand(
	string CommentId,
	string Title,
	string Description,
	string RequestingUserId) : IRequest<Result<CommentDto>>;

/// <summary>
///   Handler for updating an existing comment.
/// </summary>
public sealed class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result<CommentDto>>
{
	private readonly IRepository<Comment> _repository;
	private readonly ILogger<UpdateCommentCommandHandler> _logger;

	public UpdateCommentCommandHandler(
		IRepository<Comment> repository,
		ILogger<UpdateCommentCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<CommentDto>> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating comment with ID: {CommentId}", request.CommentId);

		var result = await _repository.GetByIdAsync(request.CommentId, cancellationToken);

		if (result.Failure || result.Value is null)
		{
			_logger.LogWarning("Comment not found with ID: {CommentId}", request.CommentId);
			return Result.Fail<CommentDto>("Comment not found", ResultErrorCode.NotFound);
		}

		var existingComment = result.Value;

		// Check if the requesting user is the author
		if (existingComment.Author.Id != request.RequestingUserId)
		{
			_logger.LogWarning("User {UserId} attempted to update comment {CommentId} owned by {OwnerId}",
				request.RequestingUserId, request.CommentId, existingComment.Author.Id);
			return Result.Fail<CommentDto>("Only the comment author can edit this comment", ResultErrorCode.Validation);
		}

		// Update the existing tracked entity in place
		existingComment.Title = request.Title;
		existingComment.Description = request.Description;
		existingComment.DateModified = DateTime.UtcNow;

		var updateResult = await _repository.UpdateAsync(existingComment, cancellationToken);

		if (updateResult.Failure)
		{
			_logger.LogError("Failed to update comment: {Error}", updateResult.Error);
			return Result.Fail<CommentDto>(updateResult.Error ?? "Failed to update comment", updateResult.ErrorCode);
		}

		_logger.LogInformation("Successfully updated comment with ID: {CommentId}", request.CommentId);
		return Result.Ok(new CommentDto(updateResult.Value!));
	}
}
