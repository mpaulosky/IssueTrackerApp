// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AttachmentEndpoints.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using System.Security.Claims;
using Domain.Abstractions;
using Domain.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Web.Services;

namespace Web.Features;

/// <summary>
///   Extension methods for mapping attachment API endpoints.
/// </summary>
public static class AttachmentEndpoints
{
	/// <summary>
	///   Maps the attachment API endpoints.
	/// </summary>
	public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/api");

		// GET /api/issues/{issueId}/attachments - List attachments for an issue
		group.MapGet("/issues/{issueId}/attachments", GetIssueAttachmentsAsync)
			.WithName("GetIssueAttachments")
			.RequireAuthorization();

		// POST /api/issues/{issueId}/attachments - Upload attachment to an issue
		group.MapPost("/issues/{issueId}/attachments", UploadAttachmentAsync)
			.WithName("UploadAttachment")
			.RequireAuthorization()
			.DisableAntiforgery();

		// GET /api/attachments/{id} - Download attachment
		group.MapGet("/attachments/{id}", DownloadAttachmentAsync)
			.WithName("DownloadAttachment")
			.RequireAuthorization();

		// DELETE /api/attachments/{id} - Delete attachment
		group.MapDelete("/attachments/{id}", DeleteAttachmentAsync)
			.WithName("DeleteAttachment")
			.RequireAuthorization();

		return endpoints;
	}

	private static async Task<IResult> GetIssueAttachmentsAsync(
		string issueId,
		IAttachmentService attachmentService,
		CancellationToken cancellationToken)
	{
		var result = await attachmentService.GetIssueAttachmentsAsync(issueId, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.NotFound
				? Results.NotFound(new { error = result.Error })
				: Results.BadRequest(new { error = result.Error });
		}

		return Results.Ok(result.Value);
	}

	private static async Task<IResult> UploadAttachmentAsync(
		string issueId,
		HttpRequest request,
		IAttachmentService attachmentService,
		ClaimsPrincipal user,
		CancellationToken cancellationToken)
	{
		if (!request.HasFormContentType)
		{
			return Results.BadRequest(new { error = "Request must be multipart/form-data" });
		}

		IFormCollection form;
		try
		{
			form = await request.ReadFormAsync(cancellationToken);
		}
		catch (InvalidDataException)
		{
			return Results.BadRequest(new { error = "No file provided" });
		}

		var file = form.Files.GetFile("file");

		if (file == null || file.Length == 0)
		{
			return Results.BadRequest(new { error = "No file provided" });
		}

		// Validate file size
		if (file.Length > FileValidationConstants.MAX_FILE_SIZE)
		{
			return Results.BadRequest(new
			{
				error = $"File size exceeds maximum allowed size of {FileValidationConstants.MAX_FILE_SIZE / (1024 * 1024)}MB"
			});
		}

		// Validate content type
		if (!FileValidationConstants.IsAllowedContentType(file.ContentType))
		{
			return Results.BadRequest(new
			{
				error = $"File type '{file.ContentType}' is not allowed. Allowed types: {string.Join(", ", FileValidationConstants.ALLOWED_CONTENT_TYPES)}"
			});
		}

		// Get user info from claims
		var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
		var userName = user.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
		var userEmail = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
		var uploadedBy = new UserDto(userId, userName, userEmail);

		await using var stream = file.OpenReadStream();
		var result = await attachmentService.AddAttachmentAsync(
			issueId,
			stream,
			file.FileName,
			file.ContentType,
			file.Length,
			uploadedBy,
			cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.NotFound
				? Results.NotFound(new { error = result.Error })
				: Results.BadRequest(new { error = result.Error });
		}

		return Results.Created($"/api/attachments/{result.Value!.Id}", result.Value);
	}

	private static async Task<IResult> DownloadAttachmentAsync(
		string id,
		IAttachmentService attachmentService,
		IFileStorageService fileStorageService,
		Persistence.MongoDb.IssueTrackerDbContext dbContext,
		CancellationToken cancellationToken)
	{
		// Get attachment metadata from database
		var attachment = await dbContext.Attachments
			.FirstOrDefaultAsync(a => a.Id == MongoDB.Bson.ObjectId.Parse(id), cancellationToken);

		if (attachment == null)
		{
			return Results.NotFound(new { error = "Attachment not found" });
		}

		try
		{
			var stream = await fileStorageService.DownloadAsync(attachment.BlobUrl, cancellationToken);
			return Results.File(stream, attachment.ContentType, attachment.FileName);
		}
		catch (FileNotFoundException)
		{
			return Results.NotFound(new { error = "Attachment file not found" });
		}
	}

	private static async Task<IResult> DeleteAttachmentAsync(
		string id,
		IAttachmentService attachmentService,
		ClaimsPrincipal user,
		CancellationToken cancellationToken)
	{
		var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
		var isAdmin = user.IsInRole("Admin");

		var result = await attachmentService.DeleteAttachmentAsync(id, userId, isAdmin, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode switch
			{
				ResultErrorCode.NotFound => Results.NotFound(new { error = result.Error }),
				ResultErrorCode.Validation when result.Error?.Contains("Unauthorized") == true =>
					Results.Forbid(),
				_ => Results.BadRequest(new { error = result.Error })
			};
		}

		return Results.NoContent();
	}
}
