// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GridFsStorageDeleteTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.GridFs.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.GridFs.Integration;

/// <summary>
///   Integration tests for GridFsStorageService delete functionality.
/// </summary>
[Collection("GridFsIntegration")]
public sealed class GridFsStorageDeleteTests
{
	private readonly MongoDbGridFsFixture _fixture;

	public GridFsStorageDeleteTests(MongoDbGridFsFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task DeleteAsync_AfterUpload_ShouldRemoveFile()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var blobUrl = await service.UploadAsync(
			new MemoryStream("Content to be deleted"u8.ToArray()),
			"delete-test.txt",
			"text/plain");

		// Act
		await service.DeleteAsync(blobUrl);

		// Assert — file no longer exists in GridFS
		Func<Task> act = async () => await service.DownloadAsync(blobUrl);
		await act.Should().ThrowAsync<FileNotFoundException>();
	}

	[Fact]
	public async Task DeleteAsync_WithNonExistentUrl_ShouldNotThrowException()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var nonExistentUrl = "/api/attachments/000000000000000000000001";

		// Act
		Func<Task> act = async () => await service.DeleteAsync(nonExistentUrl);

		// Assert — deletion of a non-existent file is idempotent
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task DeleteAsync_MultipleTimes_ShouldBeIdempotent()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var blobUrl = await service.UploadAsync(
			new MemoryStream("Idempotent delete test"u8.ToArray()),
			"idempotent-delete.txt",
			"text/plain");

		// Act
		await service.DeleteAsync(blobUrl);
		Func<Task> act = async () => await service.DeleteAsync(blobUrl);

		// Assert — second delete should not throw
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task DeleteAsync_ShouldOnlyDeleteSpecifiedFile()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var blobUrl1 = await service.UploadAsync(
			new MemoryStream("First file"u8.ToArray()),
			"first.txt",
			"text/plain");
		var blobUrl2 = await service.UploadAsync(
			new MemoryStream("Second file"u8.ToArray()),
			"second.txt",
			"text/plain");

		// Act
		await service.DeleteAsync(blobUrl1);

		// Assert — first file gone, second file still accessible
		Func<Task> actFirst = async () => await service.DownloadAsync(blobUrl1);
		await actFirst.Should().ThrowAsync<FileNotFoundException>();

		var secondStream = await service.DownloadAsync(blobUrl2);
		secondStream.Should().NotBeNull();
	}
}
