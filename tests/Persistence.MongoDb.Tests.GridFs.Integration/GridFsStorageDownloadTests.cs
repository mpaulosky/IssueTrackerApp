// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GridFsStorageDownloadTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.GridFs.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.GridFs.Integration;

/// <summary>
///   Integration tests for GridFsStorageService download functionality.
/// </summary>
[Collection("GridFsIntegration")]
public sealed class GridFsStorageDownloadTests
{
	private readonly MongoDbGridFsFixture _fixture;

	public GridFsStorageDownloadTests(MongoDbGridFsFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task DownloadAsync_AfterUpload_ShouldReturnSameContent()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var originalContent = "This is test content for download verification"u8.ToArray();
		var blobUrl = await service.UploadAsync(
			new MemoryStream(originalContent),
			"download-test.txt",
			"text/plain");

		// Act
		var downloadStream = await service.DownloadAsync(blobUrl);

		// Assert
		using var memoryStream = new MemoryStream();
		await downloadStream.CopyToAsync(memoryStream);
		memoryStream.ToArray().Should().Equal(originalContent);
	}

	[Fact]
	public async Task DownloadAsync_WithTextContent_ShouldMatchOriginal()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var originalText = "Test text content for roundtrip verification";
		var blobUrl = await service.UploadAsync(
			new MemoryStream(System.Text.Encoding.UTF8.GetBytes(originalText)),
			"text-roundtrip.txt",
			"text/plain");

		// Act
		var downloadStream = await service.DownloadAsync(blobUrl);

		// Assert
		using var reader = new StreamReader(downloadStream);
		var downloadedText = await reader.ReadToEndAsync();
		downloadedText.Should().Be(originalText);
	}

	[Fact]
	public async Task DownloadAsync_WithNonExistentUrl_ShouldThrowException()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var nonExistentUrl = "/api/attachments/000000000000000000000000";

		// Act
		Func<Task> act = async () => await service.DownloadAsync(nonExistentUrl);

		// Assert — GridFS throws FileNotFoundException for missing files
		await act.Should().ThrowAsync<FileNotFoundException>();
	}

	[Fact]
	public async Task DownloadAsync_WithBinaryContent_ShouldReturnExactBytes()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var originalBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
		var blobUrl = await service.UploadAsync(
			new MemoryStream(originalBytes),
			"binary-test.bin",
			"application/octet-stream");

		// Act
		var downloadStream = await service.DownloadAsync(blobUrl);

		// Assert
		using var memoryStream = new MemoryStream();
		await downloadStream.CopyToAsync(memoryStream);
		memoryStream.ToArray().Should().Equal(originalBytes);
	}
}
