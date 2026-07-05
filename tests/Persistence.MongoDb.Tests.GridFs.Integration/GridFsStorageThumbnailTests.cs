// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GridFsStorageThumbnailTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.GridFs.Integration
// =======================================================

using SixLabors.ImageSharp.Processing;

namespace Persistence.MongoDb.Tests.GridFs.Integration;

/// <summary>
///   Integration tests for GridFsStorageService thumbnail generation functionality.
/// </summary>
[Collection("GridFsIntegration")]
public sealed class GridFsStorageThumbnailTests
{
	private readonly MongoDbGridFsFixture _fixture;

	public GridFsStorageThumbnailTests(MongoDbGridFsFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task GenerateThumbnailAsync_WithValidImage_ShouldReturnThumbnailUrl()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var imageStream = await CreateTestImageAsync(800, 600);
		var blobUrl = await service.UploadAsync(imageStream, "original.jpg", "image/jpeg");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert
		thumbnailUrl.Should().NotBeNullOrEmpty();
		thumbnailUrl.Should().EndWith("/thumbnail");
	}

	[Fact]
	public async Task GenerateThumbnailAsync_ShouldCreateDownloadableThumbnail()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
		var imageStream = await CreateTestImageAsync(1024, 768);
		var blobUrl = await service.UploadAsync(imageStream, "large-image.jpg", "image/jpeg");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert — thumbnail can be downloaded
		thumbnailUrl.Should().NotBeNull();
		var downloadStream = await service.DownloadAsync(thumbnailUrl!);
		downloadStream.Should().NotBeNull();
	}

	[Fact]
	public async Task GenerateThumbnailAsync_ShouldResizeImageToMaxDimensions()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
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
		var service = _fixture.CreateGridFsStorageService();
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
		actualRatio.Should().BeApproximately(expectedRatio, 0.05);
	}

	[Fact]
	public async Task GenerateThumbnailAsync_WithNonImageFile_ShouldReturnNull()
	{
		// Arrange
		var service = _fixture.CreateGridFsStorageService();
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
		var service = _fixture.CreateGridFsStorageService();
		var imageStream = await CreateTestImageAsync(500, 500);
		var blobUrl = await service.UploadAsync(imageStream, "format-test.png", "image/png");

		// Act
		var thumbnailUrl = await service.GenerateThumbnailAsync(blobUrl);

		// Assert — verify JPEG magic bytes (FF D8 FF)
		thumbnailUrl.Should().NotBeNull();
		var downloadStream = await service.DownloadAsync(thumbnailUrl!);
		using var memoryStream = new MemoryStream();
		await downloadStream.CopyToAsync(memoryStream);
		var bytes = memoryStream.ToArray();

		bytes[0].Should().Be(0xFF);
		bytes[1].Should().Be(0xD8);
		bytes[2].Should().Be(0xFF);
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
