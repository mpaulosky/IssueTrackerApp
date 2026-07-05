// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageServiceUploadTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests
// =======================================================

namespace Persistence.AzureStorage.Tests;

/// <summary>
///   Unit tests for BlobStorageService upload operations.
/// </summary>
public sealed class BlobStorageServiceUploadTests
{
	[Fact]
	public async Task UploadAsync_WhenSuccessful_ShouldReturnBlobUrl()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var mockContainerClient = Substitute.For<BlobContainerClient>();
		var mockBlobClient = Substitute.For<BlobClient>();
		var expectedUri = new Uri("https://storage.example.com/container/blob-guid/test.txt");

		mockBlobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(mockContainerClient);
		mockContainerClient.GetBlobClient(Arg.Any<string>()).Returns(mockBlobClient);
		mockBlobClient.Uri.Returns(expectedUri);

		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
		var fileName = "test.txt";
		var contentType = "text/plain";

		// Act
		var result = await service.UploadAsync(content, fileName, contentType);

		// Assert
		result.Should().Be(expectedUri.ToString());
		result.Should().Contain(fileName);
	}

	[Fact]
	public async Task UploadAsync_ShouldCreateContainerIfNotExists()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var mockContainerClient = Substitute.For<BlobContainerClient>();
		var mockBlobClient = Substitute.For<BlobClient>();

		mockBlobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(mockContainerClient);
		mockContainerClient.GetBlobClient(Arg.Any<string>()).Returns(mockBlobClient);
		mockBlobClient.Uri.Returns(new Uri("https://storage.example.com/container/blob"));

		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));

		// Act
		await service.UploadAsync(content, "test.txt", "text/plain");

		// Assert
		await mockContainerClient.Received(1).CreateIfNotExistsAsync(
			PublicAccessType.None,
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UploadAsync_ShouldSetCorrectContentTypeHeader()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var mockContainerClient = Substitute.For<BlobContainerClient>();
		var mockBlobClient = Substitute.For<BlobClient>();

		mockBlobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(mockContainerClient);
		mockContainerClient.GetBlobClient(Arg.Any<string>()).Returns(mockBlobClient);
		mockBlobClient.Uri.Returns(new Uri("https://storage.example.com/container/blob"));

		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
		var contentType = "application/pdf";

		// Act
		await service.UploadAsync(content, "test.pdf", contentType);

		// Assert
		await mockBlobClient.Received(1).UploadAsync(
			Arg.Any<Stream>(),
			Arg.Is<BlobUploadOptions>(opts => opts.HttpHeaders.ContentType == contentType),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UploadAsync_ShouldLogSuccessfulUpload()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var mockContainerClient = Substitute.For<BlobContainerClient>();
		var mockBlobClient = Substitute.For<BlobClient>();

		mockBlobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(mockContainerClient);
		mockContainerClient.GetBlobClient(Arg.Any<string>()).Returns(mockBlobClient);
		mockBlobClient.Uri.Returns(new Uri("https://storage.example.com/container/blob"));

		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
		var fileName = "test.txt";

		// Act
		await service.UploadAsync(content, fileName, "text/plain");

		// Assert
		logger.Received(1).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains(fileName)),
			null,
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task UploadAsync_WhenExceptionOccurs_ShouldLogErrorAndRethrow()
	{
		// Arrange
		var mockBlobServiceClient = Substitute.For<BlobServiceClient>();
		var mockContainerClient = Substitute.For<BlobContainerClient>();
		var mockBlobClient = Substitute.For<BlobClient>();

		mockBlobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(mockContainerClient);
		mockContainerClient.GetBlobClient(Arg.Any<string>()).Returns(mockBlobClient);

		// Make UploadAsync throw an exception
		mockBlobClient.UploadAsync(
				Arg.Any<Stream>(),
				Arg.Any<BlobUploadOptions>(),
				Arg.Any<CancellationToken>())
			.Returns(Task.FromException<Azure.Response<BlobContentInfo>>(new InvalidOperationException("Storage error")));

		mockBlobClient.Uri.Returns(new Uri("https://storage.example.com/container/blob"));

		var settings = Options.Create(new BlobStorageSettings
		{
			ContainerName = "test-container"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();
		var service = new BlobStorageService(mockBlobServiceClient, settings, logger);

		using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
		var fileName = "test.txt";

		// Act
		Func<Task> act = async () => await service.UploadAsync(content, fileName, "text/plain");

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("Storage error");

		logger.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains(fileName)),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}
}
