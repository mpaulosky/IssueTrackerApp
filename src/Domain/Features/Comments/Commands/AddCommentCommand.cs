// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddCommentCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Events;
using Domain.Mappers;

namespace Domain.Features.Comments.Commands;

/// <summary>
///   Command to add a new comment to an issue.
/// </summary>
public record AddCommentCommand(
	string IssueId,
	string Title,
	string Description,
	UserDto Author) : IRequest<Result<CommentDto>>;

/// <summary>
///   Handler for adding a new comment to an issue.
/// </summary>
public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<CommentDto>>
{
	private readonly IRepository<Comment> _commentRepository;
	private readonly IRepository<Issue> _issueRepository;
	private readonly IMediator _mediator;
	private readonly ILogger<AddCommentCommandHandler> _logger;

	public AddCommentCommandHandler(
		IRepository<Comment> commentRepository,
		IRepository<Issue> issueRepository,
		IMediator mediator,
		ILogger<AddCommentCommandHandler> logger)
	{
		_commentRepository = commentRepository;
		_issueRepository = issueRepository;
		_mediator = mediator;
		_logger = logger;
	}

	public async Task<Result<CommentDto>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Adding comment to issue: {IssueId}", request.IssueId);

		// Verify issue exists
		var issueResult = await _issueRepository.GetByIdAsync(request.IssueId, cancellationToken);
		if (issueResult.Failure || issueResult.Value is null)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.IssueId);
			return Result.Fail<CommentDto>("Issue not found", ResultErrorCode.NotFound);
		}

		var issue = issueResult.Value;

		var comment = new Comment
		{
			Id = ObjectId.GenerateNewId(),
			Title = request.Title,
			Description = request.Description,
			Author = UserMapper.ToInfo(request.Author),
			IssueId = issue.Id,
			DateCreated = DateTime.UtcNow,
			Archived = false,
			IsAnswer = false
		};

		var result = await _commentRepository.AddAsync(comment, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to add comment: {Error}", result.Error);
			return Result.Fail<CommentDto>(result.Error ?? "Failed to add comment", result.ErrorCode);
		}

		_logger.LogInformation("Successfully added comment with ID: {CommentId} to issue: {IssueId}", comment.Id, request.IssueId);

		var commentDto = new CommentDto(result.Value!);

		// Publish comment added event for email notifications
		await _mediator.Publish(new CommentAddedEvent
		{
			IssueId = issue.Id,
			IssueTitle = issue.Title,
			IssueOwner = issue.Author.Id,
			Comment = commentDto
		}, cancellationToken);

		return Result.Ok(commentDto);
	}
}
