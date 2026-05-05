// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GridFsStorageService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb
// =======================================================

using Domain.Abstractions;
using Domain.Models;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Persistence.MongoDb.Services;

/// <summary>
///   Implementation of IFileStorageService using MongoDB GridFS.
/// </summary>
public sealed class GridFsStorageService : IFileStorageService
{
	private readonly IGridFSBucket _bucket;
	private readonly ILogger<GridFsStorageService> _logger;

	public GridFsStorageService(
		IMongoDatabase database,
		ILogger<GridFsStorageService> logger)
	{
		ArgumentNullException.ThrowIfNull(database);
		ArgumentNullException.ThrowIfNull(logger);

		_logger = logger;

		_bucket = new GridFSBucket(database, new GridFSBucketOptions
		{
			BucketName = "attachments",
			ChunkSizeBytes = 255 * 1024 // 255KB chunks
		});
	}

	/// <summary>
	///   Uploads a file to GridFS storage.
	/// </summary>
	public async Task<string> UploadAsync(
		Stream content,
		string fileName,
		string contentType,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
		ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

		try
		{
			var metadata = new BsonDocument
			{
				{ "contentType", contentType },
				{ "originalFileName", fileName },
				{ "fileType", "original" },
				{ "uploadedAt", DateTime.UtcNow }
			};

			var options = new GridFSUploadOptions
			{
				Metadata = metadata
			};

			var fileId = await _bucket.UploadFromStreamAsync(
				fileName,
				content,
				options,
				cancellationToken);

			_logger.LogInformation(
				"Uploaded file {FileName} to GridFS with ID {FileId}",
				fileName,
				fileId);

			return $"/api/attachments/{fileId}";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to upload file {FileName} to GridFS", fileName);
			throw;
		}
	}

	/// <summary>
	///   Generates a thumbnail for an image file.
	/// </summary>
	public async Task<string?> GenerateThumbnailAsync(
		string blobUrl,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(blobUrl);

		try
		{
			// Download original image from GridFS
			var originalFileId = ExtractFileIdFromUrl(blobUrl);
			await using var originalStream = await _bucket.OpenDownloadStreamAsync(
				originalFileId,
				cancellationToken: cancellationToken);

			// Generate thumbnail using ImageSharp
			using var image = await Image.LoadAsync(originalStream, cancellationToken);
			using var thumbnailStream = new MemoryStream();

			image.Mutate(x => x.Resize(new ResizeOptions
			{
				Size = new Size(FileValidationConstants.THUMBNAIL_WIDTH, FileValidationConstants.THUMBNAIL_HEIGHT),
				Mode = ResizeMode.Max
			}));

			await image.SaveAsJpegAsync(thumbnailStream, cancellationToken);
			thumbnailStream.Position = 0;

			// Upload thumbnail to GridFS
			var metadata = new BsonDocument
			{
				{ "contentType", "image/jpeg" },
				{ "originalFileName", "thumbnail.jpg" },
				{ "fileType", "thumbnail" },
				{ "attachmentId", originalFileId },
				{ "uploadedAt", DateTime.UtcNow }
			};

			var options = new GridFSUploadOptions
			{
				Metadata = metadata
			};

			var thumbnailFileId = await _bucket.UploadFromStreamAsync(
				"thumbnail.jpg",
				thumbnailStream,
				options,
				cancellationToken);

			_logger.LogInformation(
				"Generated thumbnail for {OriginalFileId} with ID {ThumbnailFileId}",
				originalFileId,
				thumbnailFileId);

			return $"/api/attachments/{thumbnailFileId}/thumbnail";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate thumbnail for {BlobUrl}", blobUrl);
			return null;
		}
	}

	/// <summary>
	///   Downloads a file from GridFS storage.
	/// </summary>
	public async Task<Stream> DownloadAsync(
		string blobUrl,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(blobUrl);

		try
		{
			// Detect URL format and reject non-GridFS URLs
			if (blobUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			    blobUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				throw new NotSupportedException("Azure blob URLs are not supported by GridFS storage");
			}

			if (blobUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
			{
				throw new NotSupportedException("Local file URLs are not supported by GridFS storage");
			}

			// Extract GridFS file ID from URL
			var fileId = ExtractFileIdFromUrl(blobUrl);

			_logger.LogInformation("Downloading file {FileId} from GridFS", fileId);

			return await _bucket.OpenDownloadStreamAsync(fileId, cancellationToken: cancellationToken);
		}
		catch (GridFSFileNotFoundException ex)
		{
			_logger.LogError(ex, "File not found in GridFS: {BlobUrl}", blobUrl);
			throw new FileNotFoundException($"File not found in GridFS: {blobUrl}", ex);
		}
		catch (Exception ex) when (ex is not NotSupportedException)
		{
			_logger.LogError(ex, "Failed to download file from GridFS: {BlobUrl}", blobUrl);
			throw;
		}
	}

	/// <summary>
	///   Deletes a file from GridFS storage.
	/// </summary>
	public async Task DeleteAsync(
		string blobUrl,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(blobUrl);

		try
		{
			// Extract GridFS file ID from URL
			var fileId = ExtractFileIdFromUrl(blobUrl);

			// Delete the original file
			await _bucket.DeleteAsync(fileId, cancellationToken);

			_logger.LogInformation("Deleted file {FileId} from GridFS", fileId);

			// Find and delete associated thumbnail
			var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.attachmentId", fileId);
			var thumbnailFiles = await _bucket.FindAsync(filter, cancellationToken: cancellationToken);
			var thumbnailList = await thumbnailFiles.ToListAsync(cancellationToken);

			foreach (var thumbnail in thumbnailList)
			{
				await _bucket.DeleteAsync(thumbnail.Id, cancellationToken);
				_logger.LogInformation(
					"Deleted thumbnail {ThumbnailId} associated with {FileId}",
					thumbnail.Id,
					fileId);
			}
		}
		catch (GridFSFileNotFoundException ex)
		{
			_logger.LogWarning(ex, "File not found in GridFS for deletion: {BlobUrl}", blobUrl);
			// Don't throw - deletion of non-existent file is idempotent
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to delete file from GridFS: {BlobUrl}", blobUrl);
			throw;
		}
	}

	/// <summary>
	///   Extracts the GridFS file ID from a URL.
	/// </summary>
	/// <param name="url">The URL in format /api/attachments/{id} or /api/attachments/{id}/thumbnail</param>
	/// <returns>The parsed ObjectId</returns>
	private static ObjectId ExtractFileIdFromUrl(string url)
	{
		// Expected formats:
		// - /api/attachments/{id}
		// - /api/attachments/{id}/thumbnail

		var segments = url.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

		if (segments.Length < 2 || segments[0] != "api" || segments[1] != "attachments")
		{
			throw new ArgumentException($"Invalid GridFS URL format: {url}", nameof(url));
		}

		if (segments.Length < 3)
		{
			throw new ArgumentException($"Missing file ID in URL: {url}", nameof(url));
		}

		var idSegment = segments[2];

		if (!ObjectId.TryParse(idSegment, out var fileId))
		{
			throw new ArgumentException($"Invalid ObjectId in URL: {idSegment}", nameof(url));
		}

		return fileId;
	}
}
