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

using Microsoft.EntityFrameworkCore;

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

	/// <summary>
	///   Downloads an attachment file.
	/// </summary>
	Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadAttachmentAsync(
		string attachmentId,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Downloads a thumbnail for an attachment.
	/// </summary>
	Task<Result<Stream>> DownloadThumbnailAsync(
		string attachmentId,
		CancellationToken cancellationToken = default);
}

public class AttachmentService : IAttachmentService
{
	private readonly IMediator _mediator;
	private readonly ILogger<AttachmentService> _logger;
	private readonly IFileStorageService _fileStorageService;
	private readonly Persistence.MongoDb.IIssueTrackerDbContext _dbContext;

	public AttachmentService(
		IMediator mediator,
		ILogger<AttachmentService> logger,
		IFileStorageService fileStorageService,
		Persistence.MongoDb.IIssueTrackerDbContext dbContext)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

	public async Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadAttachmentAsync(
		string attachmentId,
		CancellationToken cancellationToken = default)
	{
		try
		{
			// Get attachment metadata from database
			var attachment = await _dbContext.Attachments
				.FirstOrDefaultAsync(a => a.Id == MongoDB.Bson.ObjectId.Parse(attachmentId), cancellationToken);

			if (attachment == null)
			{
				return Result.Fail<(Stream, string, string)>(
					"Attachment not found",
					ResultErrorCode.NotFound);
			}

			var stream = await _fileStorageService.DownloadAsync(attachment.BlobUrl, cancellationToken);
			return Result.Ok((stream, attachment.ContentType, attachment.FileName));
		}
		catch (FileNotFoundException)
		{
			_logger.LogWarning("Attachment file not found for ID {AttachmentId}", attachmentId);
			return Result.Fail<(Stream, string, string)>(
				"Attachment file not found",
				ResultErrorCode.NotFound);
		}
		catch (NotSupportedException ex)
		{
			_logger.LogError(ex, "Storage provider mismatch for attachment {AttachmentId}", attachmentId);
			return Result.Fail<(Stream, string, string)>(
				ex.Message,
				ResultErrorCode.Validation);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error downloading attachment {AttachmentId}", attachmentId);
			return Result.Fail<(Stream, string, string)>($"Failed to download attachment: {ex.Message}");
		}
	}

	public async Task<Result<Stream>> DownloadThumbnailAsync(
		string attachmentId,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var stream = await _fileStorageService.DownloadAsync(
				$"/api/attachments/{attachmentId}/thumbnail",
				cancellationToken);

			return Result.Ok(stream);
		}
		catch (FileNotFoundException)
		{
			_logger.LogWarning("Thumbnail not found for attachment {AttachmentId}", attachmentId);
			return Result.Fail<Stream>(
				"Thumbnail not found",
				ResultErrorCode.NotFound);
		}
		catch (NotSupportedException ex)
		{
			_logger.LogError(ex, "Thumbnail not supported for attachment {AttachmentId}", attachmentId);
			return Result.Fail<Stream>(
				ex.Message,
				ResultErrorCode.Validation);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error downloading thumbnail for attachment {AttachmentId}", attachmentId);
			return Result.Fail<Stream>($"Failed to download thumbnail: {ex.Message}");
		}
	}
}
