// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteAttachmentCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Attachments.Commands;

/// <summary>
///   Command to delete an attachment from an issue.
/// </summary>
public record DeleteAttachmentCommand(
	string AttachmentId,
	string UserId,
	bool IsAdmin) : IRequest<Result<bool>>;

/// <summary>
///   Handler for deleting an attachment from an issue.
/// </summary>
public sealed class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand, Result<bool>>
{
	private readonly IRepository<Attachment> _repository;
	private readonly IFileStorageService _fileStorageService;
	private readonly ILogger<DeleteAttachmentCommandHandler> _logger;

	public DeleteAttachmentCommandHandler(
		IRepository<Attachment> repository,
		IFileStorageService fileStorageService,
		ILogger<DeleteAttachmentCommandHandler> logger)
	{
		_repository = repository;
		_fileStorageService = fileStorageService;
		_logger = logger;
	}

	public async Task<Result<bool>> Handle(
		DeleteAttachmentCommand request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting attachment {AttachmentId}", request.AttachmentId);

		// Get attachment
		var attachmentResult = await _repository.GetByIdAsync(request.AttachmentId, cancellationToken);

		if (attachmentResult.Failure || attachmentResult.Value == null)
		{
			_logger.LogWarning("Attachment {AttachmentId} not found", request.AttachmentId);
			return Result.Fail<bool>("Attachment not found", "NOT_FOUND");
		}

		var attachment = attachmentResult.Value;

		// Verify authorization (only uploader or admin can delete)
		if (!request.IsAdmin && attachment.UploadedBy.Id != request.UserId)
		{
			_logger.LogWarning(
				"User {UserId} attempted to delete attachment {AttachmentId} uploaded by {UploaderId}",
				request.UserId,
				request.AttachmentId,
				attachment.UploadedBy.Id);
			return Result.Fail<bool>("Unauthorized to delete this attachment", "UNAUTHORIZED");
		}

		try
		{
			// Delete from storage
			await _fileStorageService.DeleteAsync(attachment.BlobUrl, cancellationToken);

			// Delete thumbnail if exists
			if (!string.IsNullOrEmpty(attachment.ThumbnailUrl))
			{
				try
				{
					await _fileStorageService.DeleteAsync(attachment.ThumbnailUrl, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to delete thumbnail for attachment {AttachmentId}", request.AttachmentId);
				}
			}

			// Delete metadata
			var deleteResult = await _repository.DeleteAsync(request.AttachmentId, cancellationToken);

			if (deleteResult.Failure)
			{
				_logger.LogError("Failed to delete attachment metadata: {Error}", deleteResult.Error);
				return Result.Fail<bool>(
					deleteResult.Error ?? "Failed to delete attachment metadata",
					deleteResult.ErrorCode);
			}

			_logger.LogInformation("Successfully deleted attachment {AttachmentId}", request.AttachmentId);
			return Result.Ok(true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to delete attachment {AttachmentId}", request.AttachmentId);
			return Result.Fail<bool>($"Failed to delete attachment: {ex.Message}");
		}
	}
}
