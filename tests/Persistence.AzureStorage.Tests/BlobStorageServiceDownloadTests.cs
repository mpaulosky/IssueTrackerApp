// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageServiceDownloadTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests
// =======================================================

namespace Persistence.AzureStorage.Tests;

/// <summary>
///   Unit tests for BlobStorageService download operations.
///   Note: DownloadAsync creates BlobClient directly from URI, so we primarily test exception handling.
/// </summary>
public sealed class BlobStorageServiceDownloadTests
{
	[Fact]
	public async Task DownloadAsync_WhenExceptionOccurs_ShouldLogError()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		// Invalid URL will cause exception when creating BlobClient
		var invalidBlobUrl = "not-a-valid-url";

		// Act
		Func<Task> act = async () => await service.DownloadAsync(invalidBlobUrl);

		// Assert
		await act.Should().ThrowAsync<Exception>();

		logger.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains(invalidBlobUrl)),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task DownloadAsync_WhenExceptionOccurs_ShouldRethrowException()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		var invalidBlobUrl = "not-a-valid-url";

		// Act
		Func<Task> act = async () => await service.DownloadAsync(invalidBlobUrl);

		// Assert
		await act.Should().ThrowAsync<Exception>();
	}

	[Fact]
	public async Task DownloadAsync_ShouldIncludeBlobUrlInLogging()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		var blobUrl = "invalid-url-for-test";

		// Act
		try
		{
			await service.DownloadAsync(blobUrl);
		}
		catch
		{
			// Expected to fail
		}

		// Assert
		logger.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains(blobUrl)),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}
}
