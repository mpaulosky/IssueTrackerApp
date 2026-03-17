// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AttachmentService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Attachments.Commands;
using Domain.Features.Attachments.Queries;

using MediatR;

namespace Web.Services;

/// <summary>
///   Service facade for Attachment operations, wrapping MediatR calls.
/// </summary>
public interface IAttachmentService
{
	/// <summary>
	///   Gets all attachments for a specific issue.
	/// </summary>
	Task<Result<IReadOnlyList<AttachmentDto>>> GetIssueAttachmentsAsync(
		string issueId,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Adds a new attachment to an issue.
	/// </summary>
	Task<Result<AttachmentDto>> AddAttachmentAsync(
		string issueId,
		Stream fileStream,
		string fileName,
		string contentType,
		long fileSize,
		UserDto uploadedBy,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Deletes an attachment.
	/// </summary>
	Task<Result<bool>> DeleteAttachmentAsync(
		string attachmentId,
		string requestingUserId,
		bool isAdmin,
		CancellationToken cancellationToken = default);
}

public class AttachmentService : IAttachmentService
{
	private readonly IMediator _mediator;
	private readonly ILogger<AttachmentService> _logger;

	public AttachmentService(
		IMediator mediator,
		ILogger<AttachmentService> logger)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Result<IReadOnlyList<AttachmentDto>>> GetIssueAttachmentsAsync(
		string issueId,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var query = new GetIssueAttachmentsQuery(issueId);
			var result = await _mediator.Send(query, cancellationToken);

			if (result.Failure)
			{
				return Result.Fail<IReadOnlyList<AttachmentDto>>(result.Error!);
			}

			return Result.Ok((IReadOnlyList<AttachmentDto>)result.Value!.ToList());
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting attachments for issue {IssueId}", issueId);
			return Result.Fail<IReadOnlyList<AttachmentDto>>($"Failed to retrieve attachments: {ex.Message}");
		}
	}

	public async Task<Result<AttachmentDto>> AddAttachmentAsync(
		string issueId,
		Stream fileStream,
		string fileName,
		string contentType,
		long fileSize,
		UserDto uploadedBy,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var command = new AddAttachmentCommand(
				issueId,
				fileStream,
				fileName,
				contentType,
				fileSize,
				uploadedBy);

			var result = await _mediator.Send(command, cancellationToken);

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error adding attachment to issue {IssueId}", issueId);
			return Result.Fail<AttachmentDto>($"Failed to add attachment: {ex.Message}");
		}
	}

	public async Task<Result<bool>> DeleteAttachmentAsync(
		string attachmentId,
		string requestingUserId,
		bool isAdmin,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var command = new DeleteAttachmentCommand(attachmentId, requestingUserId, isAdmin);
			var result = await _mediator.Send(command, cancellationToken);

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
			return Result.Fail<bool>($"Failed to delete attachment: {ex.Message}");
		}
	}
}
