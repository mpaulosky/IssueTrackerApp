// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage
// =======================================================

namespace Persistence.AzureStorage;

/// <summary>
///   Implementation of IFileStorageService using Azure Blob Storage.
/// </summary>
public class BlobStorageService : IFileStorageService
{
	private readonly BlobServiceClient _blobServiceClient;
	private readonly BlobStorageSettings _settings;
	private readonly ILogger<BlobStorageService> _logger;

	public BlobStorageService(
		BlobServiceClient blobServiceClient,
		IOptions<BlobStorageSettings> settings,
		ILogger<BlobStorageService> logger)
	{
		_blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
		_settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<string> UploadAsync(
		Stream content,
		string fileName,
		string contentType,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
			await containerClient.CreateIfNotExistsAsync(
				PublicAccessType.None,
				cancellationToken: cancellationToken);

			// Generate unique blob name
			var blobName = $"{Guid.NewGuid()}/{fileName}";
			var blobClient = containerClient.GetBlobClient(blobName);

			// Upload with metadata
			var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
			await blobClient.UploadAsync(
				content,
				new BlobUploadOptions { HttpHeaders = blobHttpHeaders },
				cancellationToken);

			_logger.LogInformation("Uploaded file {FileName} to blob storage as {BlobName}", fileName, blobName);

			return blobClient.Uri.ToString();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to upload file {FileName}", fileName);
			throw;
		}
	}

	public async Task<Stream> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default)
	{
		try
		{
			var blobClient = new BlobClient(new Uri(blobUrl));
			var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

			_logger.LogInformation("Downloaded blob from {BlobUrl}", blobUrl);

			return response.Value.Content;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to download blob from {BlobUrl}", blobUrl);
			throw;
		}
	}

	public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
	{
		try
		{
			var blobClient = new BlobClient(new Uri(blobUrl));
			await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

			_logger.LogInformation("Deleted blob at {BlobUrl}", blobUrl);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to delete blob at {BlobUrl}", blobUrl);
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
			var originalStream = await DownloadAsync(blobUrl, cancellationToken);

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

			// Upload thumbnail
			var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ThumbnailContainerName);
			await containerClient.CreateIfNotExistsAsync(
				PublicAccessType.None,
				cancellationToken: cancellationToken);

			var blobName = $"{Guid.NewGuid()}/thumbnail.jpg";
			var blobClient = containerClient.GetBlobClient(blobName);

			var blobHttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" };
			await blobClient.UploadAsync(
				thumbnailStream,
				new BlobUploadOptions { HttpHeaders = blobHttpHeaders },
				cancellationToken);

			_logger.LogInformation("Generated thumbnail for {BlobUrl}", blobUrl);

			return blobClient.Uri.ToString();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate thumbnail for {BlobUrl}", blobUrl);
			return null;
		}
	}
}
