// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageDownloadTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests.Integration
// =======================================================

namespace Persistence.AzureStorage.Tests.Integration;

/// <summary>
///   Integration tests for BlobStorageService download functionality.
/// </summary>
[Collection("Azurite")]
public sealed class BlobStorageDownloadTests
{
	private readonly AzuriteFixture _fixture;

	public BlobStorageDownloadTests(AzuriteFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task DownloadAsync_AfterUpload_ShouldReturnSameContent()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var originalContent = "This is test content for download verification"u8.ToArray();
		var uploadStream = new MemoryStream(originalContent);
		var fileName = "download-test.txt";

		var blobUrl = await service.UploadAsync(uploadStream, fileName, "text/plain");

		// Act
		var downloadStream = await service.DownloadAsync(blobUrl);

		// Assert
		using var memoryStream = new MemoryStream();
		await downloadStream.CopyToAsync(memoryStream);
		var downloadedContent = memoryStream.ToArray();
		downloadedContent.Should().Equal(originalContent);
	}

	[Fact]
	public async Task DownloadAsync_WithTextContent_ShouldMatchOriginal()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var originalText = "Test text content for roundtrip verification";
		var uploadStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(originalText));
		var fileName = "text-roundtrip.txt";

		var blobUrl = await service.UploadAsync(uploadStream, fileName, "text/plain");

		// Act
		var downloadStream = await service.DownloadAsync(blobUrl);

		// Assert
		using var reader = new StreamReader(downloadStream);
		var downloadedText = await reader.ReadToEndAsync();
		downloadedText.Should().Be(originalText);
	}

	[Fact]
	public async Task DownloadAsync_WithNonExistentBlob_ShouldThrowException()
	{
		// Arrange
		var service = _fixture.CreateBlobStorageService();
		var nonExistentUrl = "http://127.0.0.1:10000/devstoreaccount1/nonexistent/blob.txt";

		// Act
		Func<Task> act = async () => await service.DownloadAsync(nonExistentUrl);

		// Assert
		await act.Should().ThrowAsync<Exception>();
	}

	[Fact]
	public async Task DownloadAsync_WithBinaryContent_ShouldReturnExactBytes()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var originalBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
		var uploadStream = new MemoryStream(originalBytes);
		var fileName = "binary-test.bin";

		var blobUrl = await service.UploadAsync(uploadStream, fileName, "application/octet-stream");

		// Act
		var downloadStream = await service.DownloadAsync(blobUrl);

		// Assert
		using var memoryStream = new MemoryStream();
		await downloadStream.CopyToAsync(memoryStream);
		var downloadedBytes = memoryStream.ToArray();
		downloadedBytes.Should().Equal(originalBytes);
	}
}
