// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageUploadTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests.Integration
// =======================================================

namespace Persistence.AzureStorage.Tests.Integration;

/// <summary>
///   Integration tests for BlobStorageService upload functionality.
/// </summary>
[Collection("Azurite")]
public sealed class BlobStorageUploadTests
{
	private readonly AzuriteFixture _fixture;

	public BlobStorageUploadTests(AzuriteFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task UploadAsync_WithTextFile_ShouldReturnBlobUrl()
	{
		// Arrange
		var service = _fixture.CreateBlobStorageService();
		var content = new MemoryStream("Test file content"u8.ToArray());
		var fileName = "test.txt";
		var contentType = "text/plain";

		// Act
		var blobUrl = await service.UploadAsync(content, fileName, contentType);

		// Assert
		blobUrl.Should().NotBeNullOrEmpty();
		blobUrl.Should().Contain(fileName);
	}

	[Fact]
	public async Task UploadAsync_ShouldCreateBlobInContainer()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var content = new MemoryStream("Test content for verification"u8.ToArray());
		var fileName = "verify.txt";
		var contentType = "text/plain";

		// Act
		var blobUrl = await service.UploadAsync(content, fileName, contentType);

		// Assert - use authenticated client from fixture
		var blobServiceClient = _fixture.CreateBlobServiceClient();
		var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
		var exists = await containerClient.ExistsAsync();
		exists.Value.Should().BeTrue();

		// Extract blob name from URL - Azurite format: http://host/account/container/guid/filename
		// Skip account name and container name to get blob path
		var uri = new Uri(blobUrl);
		var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var blobName = string.Join("/", segments.Skip(2)); // Skip account + container
		
		var blobClient = containerClient.GetBlobClient(blobName);
		var blobExists = await blobClient.ExistsAsync();
		blobExists.Value.Should().BeTrue();
	}

	[Fact]
	public async Task UploadAsync_WithSpecificContentType_ShouldSetCorrectContentType()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var content = new MemoryStream("{\"test\":\"json\"}"u8.ToArray());
		var fileName = "data.json";
		var contentType = "application/json";

		// Act
		var blobUrl = await service.UploadAsync(content, fileName, contentType);

		// Assert - Azurite format: http://host/account/container/guid/filename
		var uri = new Uri(blobUrl);
		var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var blobName = string.Join("/", segments.Skip(2)); // Skip account + container
		
		var blobServiceClient = _fixture.CreateBlobServiceClient();
		var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
		var blobClient = containerClient.GetBlobClient(blobName);
		var properties = await blobClient.GetPropertiesAsync();
		properties.Value.ContentType.Should().Be(contentType);
	}

	[Fact]
	public async Task UploadAsync_ShouldCreateContainerAutomatically()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var content = new MemoryStream("Auto-create container test"u8.ToArray());
		var fileName = "autotest.txt";
		var contentType = "text/plain";

		// Act
		var blobUrl = await service.UploadAsync(content, fileName, contentType);

		// Assert
		var blobServiceClient = _fixture.CreateBlobServiceClient();
		var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
		var exists = await containerClient.ExistsAsync();
		exists.Value.Should().BeTrue();
		blobUrl.Should().Contain(containerName);
	}

	[Fact]
	public async Task UploadAsync_WithMultipleFiles_ShouldGenerateUniqueBlobNames()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var fileName = "duplicate.txt";

		// Act
		var blobUrl1 = await service.UploadAsync(
			new MemoryStream("First upload"u8.ToArray()),
			fileName,
			"text/plain");
		var blobUrl2 = await service.UploadAsync(
			new MemoryStream("Second upload"u8.ToArray()),
			fileName,
			"text/plain");

		// Assert
		blobUrl1.Should().NotBe(blobUrl2);
		blobUrl1.Should().Contain(fileName);
		blobUrl2.Should().Contain(fileName);
	}
}
