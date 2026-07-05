// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageDeleteTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests.Integration
// =======================================================

namespace Persistence.AzureStorage.Tests.Integration;

/// <summary>
///   Integration tests for BlobStorageService delete functionality.
/// </summary>
[Collection("Azurite")]
public sealed class BlobStorageDeleteTests
{
	private readonly AzuriteFixture _fixture;

	public BlobStorageDeleteTests(AzuriteFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task DeleteAsync_AfterUpload_ShouldRemoveBlob()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var content = new MemoryStream("Content to be deleted"u8.ToArray());
		var fileName = "delete-test.txt";

		var blobUrl = await service.UploadAsync(content, fileName, "text/plain");

		// Act
		await service.DeleteAsync(blobUrl);

		// Assert - use authenticated client from fixture
		var blobServiceClient = _fixture.CreateBlobServiceClient();
		var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
		var blobClient = containerClient.GetBlobClient(fileName);
		var exists = await blobClient.ExistsAsync();
		exists.Value.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteAsync_WithNonExistentBlob_ShouldNotThrowException()
	{
		// Arrange
		var service = _fixture.CreateBlobStorageService();
		var nonExistentUrl = "http://127.0.0.1:10000/devstoreaccount1/test-container/nonexistent-blob.txt";

		// Act
		Func<Task> act = async () => await service.DeleteAsync(nonExistentUrl);

		// Assert
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task DeleteAsync_MultipleTimes_ShouldBeIdempotent()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var content = new MemoryStream("Idempotent delete test"u8.ToArray());
		var fileName = "idempotent-delete.txt";

		var blobUrl = await service.UploadAsync(content, fileName, "text/plain");

		// Act
		await service.DeleteAsync(blobUrl);
		Func<Task> act = async () => await service.DeleteAsync(blobUrl);

		// Assert
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task DeleteAsync_ShouldOnlyDeleteSpecifiedBlob()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);

		var blobUrl1 = await service.UploadAsync(
			new MemoryStream("First blob"u8.ToArray()),
			"first.txt",
			"text/plain");
		var blobUrl2 = await service.UploadAsync(
			new MemoryStream("Second blob"u8.ToArray()),
			"second.txt",
			"text/plain");

		// Act
		await service.DeleteAsync(blobUrl1);

		// Assert - Azurite format: http://host/account/container/guid/filename
		var blobServiceClient = _fixture.CreateBlobServiceClient();
		var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

		// Extract blob1 name from URL
		var uri1 = new Uri(blobUrl1);
		var segments1 = uri1.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var blobName1 = string.Join("/", segments1.Skip(2)); // Skip account + container
		var blobClient1 = containerClient.GetBlobClient(blobName1);
		var exists1 = await blobClient1.ExistsAsync();
		exists1.Value.Should().BeFalse();

		// Extract blob2 name from URL
		var uri2 = new Uri(blobUrl2);
		var segments2 = uri2.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var blobName2 = string.Join("/", segments2.Skip(2)); // Skip account + container
		var blobClient2 = containerClient.GetBlobClient(blobName2);
		var exists2 = await blobClient2.ExistsAsync();
		exists2.Value.Should().BeTrue();
	}
}
