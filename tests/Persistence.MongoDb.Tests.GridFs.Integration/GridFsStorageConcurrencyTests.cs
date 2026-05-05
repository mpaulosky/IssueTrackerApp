// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GridFsStorageConcurrencyTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.GridFs.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.GridFs.Integration;

/// <summary>
///   Integration tests for GridFsStorageService concurrent operations.
/// </summary>
[Collection("GridFsIntegration")]
public sealed class GridFsStorageConcurrencyTests
{
	private readonly MongoDbGridFsFixture _fixture;

	public GridFsStorageConcurrencyTests(MongoDbGridFsFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task UploadAsync_WithMultipleConcurrentUploads_ShouldAllSucceed()
	{
		// Arrange
		var uploadTasks = Enumerable.Range(0, 10)
			.Select(i =>
			{
				var service = _fixture.CreateGridFsStorageService();
				var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Concurrent upload {i}"));
				return service.UploadAsync(content, $"concurrent-{i}.txt", "text/plain");
			})
			.ToList();

		// Act
		var blobUrls = await Task.WhenAll(uploadTasks);

		// Assert
		blobUrls.Should().HaveCount(10);
		blobUrls.Should().OnlyHaveUniqueItems();
		blobUrls.Should().AllSatisfy(url => url.Should().NotBeNullOrEmpty());
	}

	[Fact]
	public async Task UploadAsync_ConcurrentUploadsToSameDatabase_ShouldAllSucceed()
	{
		// Arrange — all tasks share the same service instance
		var service = _fixture.CreateGridFsStorageService();

		// Act
		var uploadTasks = Enumerable.Range(0, 10)
			.Select(i =>
			{
				var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Container test {i}"));
				return service.UploadAsync(content, $"same-db-{i}.txt", "text/plain");
			})
			.ToList();

		var blobUrls = await Task.WhenAll(uploadTasks);

		// Assert
		blobUrls.Should().HaveCount(10);
		blobUrls.Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public async Task DownloadAsync_ConcurrentDownloads_ShouldAllSucceed()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();

		var blobUrls = await Task.WhenAll(Enumerable.Range(0, 5)
			.Select(i =>
			{
				var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Download test {i}"));
				return service.UploadAsync(content, $"download-{i}.txt", "text/plain");
			}));

		// Act
		var downloadTasks = blobUrls.Select(url => service.DownloadAsync(url)).ToList();
		var streams = await Task.WhenAll(downloadTasks);

		// Assert
		streams.Should().HaveCount(5);
		streams.Should().AllSatisfy(stream => stream.Should().NotBeNull());
	}

	[Fact]
	public async Task UploadAndDownload_Concurrent_ShouldAllComplete()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var seedUrl = await service.UploadAsync(
			new MemoryStream("Upload and download test"u8.ToArray()),
			"roundtrip.txt",
			"text/plain");

		// Act — mixed concurrent uploads and downloads
		var tasks = new List<Task>();

		for (var i = 0; i < 5; i++)
		{
			var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Upload {i}"));
			tasks.Add(service.UploadAsync(content, $"parallel-upload-{i}.txt", "text/plain"));
		}

		for (var i = 0; i < 5; i++)
		{
			tasks.Add(service.DownloadAsync(seedUrl));
		}

		await Task.WhenAll(tasks);

		// Assert
		tasks.Should().AllSatisfy(task => task.IsCompletedSuccessfully.Should().BeTrue());
	}

	[Fact]
	public async Task DeleteAsync_ConcurrentDeletes_ShouldAllSucceed()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();

		var blobUrls = await Task.WhenAll(Enumerable.Range(0, 5)
			.Select(i =>
			{
				var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Delete test {i}"));
				return service.UploadAsync(content, $"delete-concurrent-{i}.txt", "text/plain");
			}));

		// Act
		await Task.WhenAll(blobUrls.Select(url => service.DeleteAsync(url)));

		// Assert — every file is gone
		foreach (var url in blobUrls)
		{
			Func<Task> act = async () => await service.DownloadAsync(url);
			await act.Should().ThrowAsync<FileNotFoundException>();
		}
	}
}
