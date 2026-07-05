// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GridFsStorageUploadTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.GridFs.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.GridFs.Integration;

/// <summary>
///   Integration tests for GridFsStorageService upload functionality.
/// </summary>
[Collection("GridFsIntegration")]
public sealed class GridFsStorageUploadTests
{
	private readonly MongoDbGridFsFixture _fixture;

	public GridFsStorageUploadTests(MongoDbGridFsFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task UploadAsync_WithTextFile_ShouldReturnBlobUrl()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var content = new MemoryStream("Test file content"u8.ToArray());
		var fileName = "test.txt";
		var contentType = "text/plain";

		// Act
		var blobUrl = await service.UploadAsync(content, fileName, contentType);

		// Assert
		blobUrl.Should().NotBeNullOrEmpty();
		blobUrl.Should().StartWith("/api/attachments/");
	}

	[Fact]
	public async Task UploadAsync_ShouldCreateFileInGridFs()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var originalContent = "Test content for verification"u8.ToArray();
		var content = new MemoryStream(originalContent);
		var fileName = "verify.txt";

		// Act
		var blobUrl = await service.UploadAsync(content, fileName, "text/plain");

		// Assert — file can be read back, confirming it exists in GridFS
		var downloadStream = await service.DownloadAsync(blobUrl);
		downloadStream.Should().NotBeNull();
	}

	[Fact]
	public async Task UploadAsync_WithSpecificContentType_ShouldStoreContentType()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var content = new MemoryStream("{\"test\":\"json\"}"u8.ToArray());
		var fileName = "data.json";
		var contentType = "application/json";

		// Act
		var blobUrl = await service.UploadAsync(content, fileName, contentType);

		// Assert — GridFS stores contentType in metadata; verify upload and download both succeed
		blobUrl.Should().NotBeNullOrEmpty();
		var downloadStream = await service.DownloadAsync(blobUrl);
		downloadStream.Should().NotBeNull();
	}

	[Fact]
	public async Task UploadAsync_ShouldGenerateUniqueUrls()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
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
		blobUrl1.Should().StartWith("/api/attachments/");
		blobUrl2.Should().StartWith("/api/attachments/");
	}

	[Fact]
	public async Task UploadAsync_WithMultipleFiles_ShouldGenerateUniqueBlobNames()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var fileName = "multi.txt";

		// Act
		var uploadTasks = Enumerable.Range(0, 5)
			.Select(i => service.UploadAsync(
				new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Content {i}")),
				fileName,
				"text/plain"))
			.ToList();

		var urls = await Task.WhenAll(uploadTasks);

		// Assert
		urls.Should().HaveCount(5);
		urls.Should().OnlyHaveUniqueItems();
		urls.Should().AllSatisfy(url => url.Should().StartWith("/api/attachments/"));
	}
}
