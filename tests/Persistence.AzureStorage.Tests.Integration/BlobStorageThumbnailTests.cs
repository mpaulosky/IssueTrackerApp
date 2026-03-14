// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageThumbnailTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests.Integration
// =======================================================

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Persistence.AzureStorage.Tests.Integration;

/// <summary>
///   Integration tests for BlobStorageService thumbnail generation functionality.
/// </summary>
[Collection("Azurite")]
public sealed class BlobStorageThumbnailTests
{
	private readonly AzuriteFixture _fixture;

	public BlobStorageThumbnailTests(AzuriteFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task GenerateThumbnailAsync_WithValidImage_ShouldReturnThumbnailUrl()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var thumbnailContainerName = $"test-thumb-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName, thumbnailContainerName);

		var imageStream = await CreateTestImageAsync(800, 600);
		var blobUrl = await service.UploadAsync(imageStream, "original.jpg", "image/jpeg");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert
		thumbnailUrl.Should().NotBeNullOrEmpty();
		thumbnailUrl.Should().Contain(thumbnailContainerName);
	}

	[Fact]
	public async Task GenerateThumbnailAsync_ShouldCreateThumbnailBlob()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var thumbnailContainerName = $"test-thumb-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName, thumbnailContainerName);

		var imageStream = await CreateTestImageAsync(1024, 768);
		var blobUrl = await service.UploadAsync(imageStream, "large-image.jpg", "image/jpeg");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert
		thumbnailUrl.Should().NotBeNull();
		var thumbnailClient = new BlobClient(new Uri(thumbnailUrl!));
		var exists = await thumbnailClient.ExistsAsync();
		exists.Value.Should().BeTrue();
	}

	[Fact]
	public async Task GenerateThumbnailAsync_ShouldResizeImageToMaxDimensions()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var thumbnailContainerName = $"test-thumb-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName, thumbnailContainerName);

		var imageStream = await CreateTestImageAsync(800, 600);
		var blobUrl = await service.UploadAsync(imageStream, "resize-test.jpg", "image/jpeg");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert
		thumbnailUrl.Should().NotBeNull();
		var downloadStream = await service.DownloadAsync(thumbnailUrl!);
		using var thumbnailImage = await Image.LoadAsync(downloadStream);
		
		thumbnailImage.Width.Should().BeLessThanOrEqualTo(FileValidationConstants.THUMBNAIL_WIDTH);
		thumbnailImage.Height.Should().BeLessThanOrEqualTo(FileValidationConstants.THUMBNAIL_HEIGHT);
	}

	[Fact]
	public async Task GenerateThumbnailAsync_WithPortraitImage_ShouldMaintainAspectRatio()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var thumbnailContainerName = $"test-thumb-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName, thumbnailContainerName);

		var imageStream = await CreateTestImageAsync(400, 800);
		var blobUrl = await service.UploadAsync(imageStream, "portrait.jpg", "image/jpeg");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert
		thumbnailUrl.Should().NotBeNull();
		var downloadStream = await service.DownloadAsync(thumbnailUrl!);
		using var thumbnailImage = await Image.LoadAsync(downloadStream);
		
		thumbnailImage.Width.Should().BeLessThanOrEqualTo(FileValidationConstants.THUMBNAIL_WIDTH);
		thumbnailImage.Height.Should().BeLessThanOrEqualTo(FileValidationConstants.THUMBNAIL_HEIGHT);
		
		var expectedRatio = 400.0 / 800.0;
		var actualRatio = (double)thumbnailImage.Width / thumbnailImage.Height;
		actualRatio.Should().BeApproximately(expectedRatio, 0.01);
	}

	[Fact]
	public async Task GenerateThumbnailAsync_WithNonImageFile_ShouldReturnNull()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var thumbnailContainerName = $"test-thumb-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName, thumbnailContainerName);

		var textStream = new MemoryStream("Not an image"u8.ToArray());
		var blobUrl = await service.UploadAsync(textStream, "notimage.txt", "text/plain");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert
		thumbnailUrl.Should().BeNull();
	}

	[Fact]
	public async Task GenerateThumbnailAsync_ShouldSaveAsJpeg()
	{
		// Arrange
		var containerName = $"test-{Guid.NewGuid():N}";
		var thumbnailContainerName = $"test-thumb-{Guid.NewGuid():N}";
		var service = _fixture.CreateBlobStorageService(containerName, thumbnailContainerName);

		var imageStream = await CreateTestImageAsync(500, 500);
		var blobUrl = await service.UploadAsync(imageStream, "format-test.png", "image/png");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert
		thumbnailUrl.Should().NotBeNull();
		var thumbnailClient = new BlobClient(new Uri(thumbnailUrl!));
		var properties = await thumbnailClient.GetPropertiesAsync();
		properties.Value.ContentType.Should().Be("image/jpeg");
	}

	private static async Task<MemoryStream> CreateTestImageAsync(int width, int height)
	{
		using var image = new Image<Rgba32>(width, height);
		image.Mutate(x => x.BackgroundColor(Color.Blue));
		
		var stream = new MemoryStream();
		await image.SaveAsJpegAsync(stream);
		stream.Position = 0;
		return stream;
	}
}
