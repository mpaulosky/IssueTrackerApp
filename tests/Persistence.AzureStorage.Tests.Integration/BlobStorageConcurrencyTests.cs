// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageConcurrencyTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests.Integration
// =======================================================

namespace Persistence.AzureStorage.Tests.Integration;

/// <summary>
///   Integration tests for BlobStorageService concurrent operations.
/// </summary>
[Collection("Azurite")]
public sealed class BlobStorageConcurrencyTests
{
	private readonly AzuriteFixture _fixture;

	public BlobStorageConcurrencyTests(AzuriteFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task UploadAsync_WithMultipleConcurrentUploads_ShouldAllSucceed()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		var uploadTasks = new List<Task<string>>();

		// Act
		for (int i = 0; i < 10; i++)
		{
			var contentText = $"Concurrent upload {i}";
			var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contentText));
			var fileName = $"concurrent-{i}.txt";
			uploadTasks.Add(service.UploadAsync(content, fileName, "text/plain"));
		}

		var blobUrls = await Task.WhenAll(uploadTasks);

		// Assert
		blobUrls.Should().HaveCount(10);
		blobUrls.Should().OnlyHaveUniqueItems();
		blobUrls.Should().AllSatisfy(url => url.Should().NotBeNullOrEmpty());
	}

	[Fact]
	public async Task UploadAsync_ConcurrentUploadsToSameContainer_ShouldAllSucceed()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);

		// Act
		var uploadTasks = Enumerable.Range(0, 10)
			.Select(i =>
			{
				var contentText = $"Container test {i}";
				var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contentText));
				return service.UploadAsync(content, $"same-container-{i}.txt", "text/plain");
			})
			.ToList();

		var blobUrls = await Task.WhenAll(uploadTasks);

		// Assert
		blobUrls.Should().HaveCount(10);
		blobUrls.Should().OnlyHaveUniqueItems();
		
		var blobServiceClient = _fixture.CreateBlobServiceClient();
		var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
		var exists = await containerClient.ExistsAsync();
		exists.Value.Should().BeTrue();
	}

	[Fact]
	public async Task DownloadAsync_ConcurrentDownloads_ShouldAllSucceed()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		
		var uploadTasks = Enumerable.Range(0, 5)
			.Select(i =>
			{
				var contentText = $"Download test {i}";
				var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contentText));
				return service.UploadAsync(content, $"download-{i}.txt", "text/plain");
			})
			.ToList();

		var blobUrls = await Task.WhenAll(uploadTasks);

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
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		
		var uploadContent = new MemoryStream("Upload and download test"u8.ToArray());
		var blobUrl = await service.UploadAsync(uploadContent, "roundtrip.txt", "text/plain");

		// Act
		var tasks = new List<Task>();
		
		for (int i = 0; i < 5; i++)
		{
			var contentText = $"Upload {i}";
			var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contentText));
			tasks.Add(service.UploadAsync(content, $"parallel-upload-{i}.txt", "text/plain"));
		}
		
		for (int i = 0; i < 5; i++)
		{
			tasks.Add(service.DownloadAsync(blobUrl));
		}

		await Task.WhenAll(tasks);

		// Assert
		tasks.Should().AllSatisfy(task => task.IsCompletedSuccessfully.Should().BeTrue());
	}

	[Fact]
	public async Task DeleteAsync_ConcurrentDeletes_ShouldAllSucceed()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		
		var uploadTasks = Enumerable.Range(0, 5)
			.Select(i =>
			{
				var contentText = $"Delete test {i}";
				var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contentText));
				return service.UploadAsync(content, $"delete-concurrent-{i}.txt", "text/plain");
			})
			.ToList();

		var blobUrls = await Task.WhenAll(uploadTasks);

		// Act
		var deleteTasks = blobUrls.Select(url => service.DeleteAsync(url)).ToList();
		await Task.WhenAll(deleteTasks);

		// Assert
		foreach (var blobUrl in blobUrls)
		{
			var blobClient = new BlobClient(new Uri(blobUrl));
			var exists = await blobClient.ExistsAsync();
			exists.Value.Should().BeFalse();
		}
	}

	[Fact]
	public async Task ConcurrentOperations_MixedTypes_ShouldAllComplete()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName: containerName);
		
		var uploadContent = new MemoryStream("Setup blob"u8.ToArray());
		var setupBlobUrl = await service.UploadAsync(uploadContent, "setup.txt", "text/plain");

		// Act
		var tasks = new List<Task>();
		
		tasks.Add(service.UploadAsync(
			new MemoryStream("Upload 1"u8.ToArray()),
			"mixed-1.txt",
			"text/plain"));
		tasks.Add(service.DownloadAsync(setupBlobUrl));
		tasks.Add(service.UploadAsync(
			new MemoryStream("Upload 2"u8.ToArray()),
			"mixed-2.txt",
			"text/plain"));
		tasks.Add(service.DownloadAsync(setupBlobUrl));
		tasks.Add(service.UploadAsync(
			new MemoryStream("Upload 3"u8.ToArray()),
			"mixed-3.txt",
			"text/plain"));

		await Task.WhenAll(tasks);

		// Assert
		tasks.Should().AllSatisfy(task => task.IsCompletedSuccessfully.Should().BeTrue());
	}
}
