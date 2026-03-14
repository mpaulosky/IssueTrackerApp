// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddAttachmentCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Mappers;

namespace Domain.Features.Attachments.Commands;

/// <summary>
///   Command to add an attachment to an issue.
/// </summary>
public record AddAttachmentCommand(
	string IssueId,
	Stream FileContent,
	string FileName,
	string ContentType,
	long FileSize,
	UserDto UploadedBy) : IRequest<Result<AttachmentDto>>;

/// <summary>
///   Handler for adding an attachment to an issue.
/// </summary>
public sealed class AddAttachmentCommandHandler : IRequestHandler<AddAttachmentCommand, Result<AttachmentDto>>
{
	private readonly IRepository<Attachment> _repository;
	private readonly IFileStorageService _fileStorageService;
	private readonly ILogger<AddAttachmentCommandHandler> _logger;

	public AddAttachmentCommandHandler(
		IRepository<Attachment> repository,
		IFileStorageService fileStorageService,
		ILogger<AddAttachmentCommandHandler> logger)
	{
		_repository = repository;
		_fileStorageService = fileStorageService;
		_logger = logger;
	}

	public async Task<Result<AttachmentDto>> Handle(
		AddAttachmentCommand request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation(
			"Adding attachment {FileName} to issue {IssueId}",
			request.FileName,
			request.IssueId);

		try
		{
			// Upload file to storage
			var blobUrl = await _fileStorageService.UploadAsync(
				request.FileContent,
				request.FileName,
				request.ContentType,
				cancellationToken);

			// Generate thumbnail if image
			string? thumbnailUrl = null;
			if (FileValidationConstants.IsImage(request.ContentType))
			{
				try
				{
					thumbnailUrl = await _fileStorageService.GenerateThumbnailAsync(blobUrl, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to generate thumbnail for {FileName}", request.FileName);
				}
			}

			// Create attachment record
			var attachment = new Attachment
			{
				Id = ObjectId.GenerateNewId(),
				IssueId = ObjectId.Parse(request.IssueId),
				FileName = request.FileName,
				ContentType = request.ContentType,
				FileSize = request.FileSize,
				BlobUrl = blobUrl,
				ThumbnailUrl = thumbnailUrl,
				UploadedBy = UserMapper.ToInfo(request.UploadedBy),
				UploadedAt = DateTime.UtcNow
			};

			var result = await _repository.AddAsync(attachment, cancellationToken);

			if (result.Failure)
			{
				_logger.LogError("Failed to save attachment metadata: {Error}", result.Error);
				
				// Clean up uploaded file
				try
				{
					await _fileStorageService.DeleteAsync(blobUrl, cancellationToken);
					if (thumbnailUrl != null)
					{
						await _fileStorageService.DeleteAsync(thumbnailUrl, cancellationToken);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to clean up uploaded files after metadata save failure");
				}

				return Result.Fail<AttachmentDto>(
					result.Error ?? "Failed to save attachment metadata",
					result.ErrorCode);
			}

			_logger.LogInformation(
				"Successfully added attachment {AttachmentId} to issue {IssueId}",
				attachment.Id,
				request.IssueId);

			return Result.Ok(new AttachmentDto(result.Value!));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to add attachment {FileName} to issue {IssueId}", request.FileName, request.IssueId);
			return Result.Fail<AttachmentDto>($"Failed to add attachment: {ex.Message}");
		}
	}
}
