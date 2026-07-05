// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageServiceThumbnailTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests
// =======================================================

namespace Persistence.AzureStorage.Tests;

/// <summary>
///   Unit tests for BlobStorageService thumbnail generation.
///   Note: GenerateThumbnailAsync depends on DownloadAsync which creates BlobClient directly,
///   so we primarily test exception handling and null return on error.
/// </summary>
public sealed class BlobStorageServiceThumbnailTests
{
	[Fact]
	public async Task GenerateThumbnailAsync_WhenExceptionOccurs_ShouldReturnNull()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container",
			ThumbnailContainerName = "test-thumbnails"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		// Invalid URL will cause exception in DownloadAsync
		var invalidBlobUrl = "not-a-valid-url";

		// Act
		var result = await service.GenerateThumbnailAsync(invalidBlobUrl);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GenerateThumbnailAsync_WhenExceptionOccurs_ShouldLogError()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container",
			ThumbnailContainerName = "test-thumbnails"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		var invalidBlobUrl = "not-a-valid-url";

		// Act
		await service.GenerateThumbnailAsync(invalidBlobUrl);

		// Assert
		// GenerateThumbnailAsync calls DownloadAsync which logs, then logs itself
		// So we expect 2 error log calls
		logger.Received(2).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains(invalidBlobUrl)),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task GenerateThumbnailAsync_ShouldIncludeBlobUrlInLogging()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container",
			ThumbnailContainerName = "test-thumbnails"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		var blobUrl = "invalid-url-for-test";

		// Act
		await service.GenerateThumbnailAsync(blobUrl);

		// Assert
		// GenerateThumbnailAsync logs errors, and it calls DownloadAsync which also logs
		// So we expect at least 1 log call containing the blob URL
		logger.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains(blobUrl)),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}
}
