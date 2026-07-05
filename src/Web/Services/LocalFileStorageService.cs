// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LocalFileStorageService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.Models;

using SkiaSharp;

namespace Web.Services;

/// <summary>
///   Local file storage implementation for development without Azure.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
	private readonly IWebHostEnvironment _environment;
	private readonly ILogger<LocalFileStorageService> _logger;
	private const string UPLOADS_FOLDER = "uploads";
	private const string THUMBNAILS_FOLDER = "uploads/thumbnails";

	public LocalFileStorageService(
		IWebHostEnvironment environment,
		ILogger<LocalFileStorageService> logger)
	{
		_environment = environment ?? throw new ArgumentNullException(nameof(environment));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		// Ensure directories exist
		var uploadsPath = Path.Combine(_environment.WebRootPath, UPLOADS_FOLDER);
		var thumbnailsPath = Path.Combine(_environment.WebRootPath, THUMBNAILS_FOLDER);

		Directory.CreateDirectory(uploadsPath);
		Directory.CreateDirectory(thumbnailsPath);
	}

	public async Task<string> UploadAsync(
		Stream content,
		string fileName,
		string contentType,
		CancellationToken cancellationToken = default)
	{
		try
		{
			// Generate unique file name
			var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
			var filePath = Path.Combine(_environment.WebRootPath, UPLOADS_FOLDER, uniqueFileName);

			// Save file
			await using var fileStream = new FileStream(filePath, FileMode.Create);
			await content.CopyToAsync(fileStream, cancellationToken);

			_logger.LogInformation("Uploaded file {FileName} to local storage at {FilePath}", fileName, filePath);

			// Return relative URL
			return $"/{UPLOADS_FOLDER}/{uniqueFileName}";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to upload file {FileName} to local storage", fileName);
			throw;
		}
	}

	public Task<Stream> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default)
	{
		try
		{
			// Convert URL to file path
			var fileName = blobUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
			var filePath = Path.Combine(_environment.WebRootPath, fileName);

			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException($"File not found: {blobUrl}");
			}

			var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

			_logger.LogInformation("Downloaded file from {BlobUrl}", blobUrl);

			return Task.FromResult<Stream>(stream);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to download file from {BlobUrl}", blobUrl);
			throw;
		}
	}

	public Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
	{
		try
		{
			// Convert URL to file path
			var fileName = blobUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
			var filePath = Path.Combine(_environment.WebRootPath, fileName);

			if (File.Exists(filePath))
			{
				File.Delete(filePath);
				_logger.LogInformation("Deleted file at {BlobUrl}", blobUrl);
			}

			return Task.CompletedTask;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to delete file at {BlobUrl}", blobUrl);
			throw;
		}
	}

	public async Task<string?> GenerateThumbnailAsync(
		string blobUrl,
		CancellationToken cancellationToken = default)
	{
		try
		{
			// Download original image
			await using var originalStream = await DownloadAsync(blobUrl, cancellationToken);
			using var originalBitmap = SKBitmap.Decode(originalStream);

			if (originalBitmap is null || originalBitmap.Width <= 0 || originalBitmap.Height <= 0)
			{
				return null;
			}

			var scale = Math.Min(
				(float)FileValidationConstants.THUMBNAIL_WIDTH / originalBitmap.Width,
				(float)FileValidationConstants.THUMBNAIL_HEIGHT / originalBitmap.Height);

			var resizedWidth = Math.Max(1, (int)Math.Round(originalBitmap.Width * scale));
			var resizedHeight = Math.Max(1, (int)Math.Round(originalBitmap.Height * scale));

			using var resizedBitmap = originalBitmap.Resize(
				new SKImageInfo(resizedWidth, resizedHeight),
				new SKSamplingOptions(SKFilterMode.Linear));

			if (resizedBitmap is null)
			{
				return null;
			}

			// Save thumbnail
			var uniqueFileName = $"{Guid.NewGuid()}_thumbnail.jpg";
			var thumbnailPath = Path.Combine(_environment.WebRootPath, THUMBNAILS_FOLDER, uniqueFileName);

			using var image = SKImage.FromBitmap(resizedBitmap);
			using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);

			if (data is null)
			{
				return null;
			}

			await using var thumbnailFile = new FileStream(thumbnailPath, FileMode.Create, FileAccess.Write);
			data.SaveTo(thumbnailFile);

			_logger.LogInformation("Generated thumbnail for {BlobUrl}", blobUrl);

			// Return relative URL
			return $"/{THUMBNAILS_FOLDER}/{uniqueFileName}";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate thumbnail for {BlobUrl}", blobUrl);
			return null;
		}
	}
}
