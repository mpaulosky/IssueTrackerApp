// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LocalFileStorageServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Microsoft.AspNetCore.Hosting;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for <see cref="LocalFileStorageService" />.
///   All I/O operates against a fresh temporary directory created per test class
///   instance, which is deleted in <see cref="Dispose" />.
/// </summary>
public sealed class LocalFileStorageServiceTests : IDisposable
{
	private readonly string _testRoot;
	private readonly IWebHostEnvironment _environment;
	private readonly ILogger<LocalFileStorageService> _logger;

	public LocalFileStorageServiceTests()
	{
		// Each test class instance gets its own isolated temp directory.
		_testRoot = Path.Combine(Path.GetTempPath(), $"lfss-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_testRoot);

		_environment = Substitute.For<IWebHostEnvironment>();
		_environment.WebRootPath.Returns(_testRoot);

		_logger = Substitute.For<ILogger<LocalFileStorageService>>();
	}

	public void Dispose()
	{
		// Best-effort cleanup so temp files don't accumulate.
		try
		{
			if (Directory.Exists(_testRoot))
			{
				Directory.Delete(_testRoot, recursive: true);
			}
		}
		catch
		{
			// Never fail the test run on cleanup.
		}
	}

	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	private LocalFileStorageService CreateSut() => new(_environment, _logger);

	/// <summary>
	///   Creates a valid JPEG image stream using ImageSharp so
	///   GenerateThumbnailAsync has real image data to process.
	/// </summary>
	private static async Task<MemoryStream> CreateJpegStreamAsync(
		int width = 10,
		int height = 10)
	{
		using var image = new Image<Rgb24>(width, height);
		var ms = new MemoryStream();
		await image.SaveAsync(ms, new JpegEncoder());
		ms.Position = 0;
		return ms;
	}

	/// <summary>Derives a physical file path from a service-returned URL.</summary>
	private string PhysicalPath(string url) =>
		Path.Combine(_testRoot, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

	// -----------------------------------------------------------------------
	// Constructor
	// -----------------------------------------------------------------------

	#region Constructor

	[Fact]
	public void Constructor_Should_CreateUploadsDirectory_Under_WebRootPath()
	{
		// Act
		_ = CreateSut();

		// Assert
		Directory.Exists(Path.Combine(_testRoot, "uploads")).Should().BeTrue();
	}

	[Fact]
	public void Constructor_Should_CreateThumbnailsDirectory_Under_WebRootPath()
	{
		// Act
		_ = CreateSut();

		// Assert
		Directory.Exists(Path.Combine(_testRoot, "uploads", "thumbnails")).Should().BeTrue();
	}

	[Fact]
	public void Constructor_Should_Throw_When_EnvironmentIsNull()
	{
		// Act
		var act = () => new LocalFileStorageService(null!, _logger);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("environment");
	}

	[Fact]
	public void Constructor_Should_Throw_When_LoggerIsNull()
	{
		// Act
		var act = () => new LocalFileStorageService(_environment, null!);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("logger");
	}

	#endregion

	// -----------------------------------------------------------------------
	// UploadAsync
	// -----------------------------------------------------------------------

	#region UploadAsync

	[Fact]
	public async Task UploadAsync_Should_ReturnUrl_StartingWithSlashUploads()
	{
		// Arrange
		var sut = CreateSut();
		using var content = new MemoryStream("hello"u8.ToArray());

		// Act
		var url = await sut.UploadAsync(content, "test.txt", "text/plain");

		// Assert
		url.Should().StartWith("/uploads/");
	}

	[Fact]
	public async Task UploadAsync_Should_ReturnUrl_ContainingOriginalFileName()
	{
		// Arrange
		var sut = CreateSut();
		const string fileName = "photo.png";
		using var content = new MemoryStream(new byte[] { 1, 2, 3 });

		// Act
		var url = await sut.UploadAsync(content, fileName, "image/png");

		// Assert — the URL ends with _{originalFileName}
		url.Should().EndWith($"_{fileName}");
	}

	[Fact]
	public async Task UploadAsync_Should_CreatePhysicalFileOnDisk()
	{
		// Arrange
		var sut = CreateSut();
		using var content = new MemoryStream(new byte[] { 10, 20, 30 });

		// Act
		var url = await sut.UploadAsync(content, "data.bin", "application/octet-stream");

		// Assert
		File.Exists(PhysicalPath(url)).Should().BeTrue();
	}

	[Fact]
	public async Task UploadAsync_Should_PersistExactStreamContentToFile()
	{
		// Arrange
		var sut = CreateSut();
		var original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
		using var content = new MemoryStream(original);

		// Act
		var url = await sut.UploadAsync(content, "payload.bin", "application/octet-stream");

		// Assert
		var written = await File.ReadAllBytesAsync(PhysicalPath(url));
		written.Should().Equal(original);
	}

	[Fact]
	public async Task UploadAsync_Should_RethrowException_When_StreamReadFails()
	{
		// Arrange — dispose the stream so CopyToAsync throws ObjectDisposedException
		var sut = CreateSut();
		var content = new MemoryStream(new byte[] { 1, 2, 3 });
		content.Dispose();

		// Act
		var act = () => sut.UploadAsync(content, "file.txt", "text/plain");

		// Assert — exception must not be swallowed by the service
		await act.Should().ThrowAsync<ObjectDisposedException>();
	}

	[Fact]
	public async Task UploadAsync_Should_HonourCancellationToken_And_ReturnUrl_When_NotCancelled()
	{
		// Arrange
		var sut = CreateSut();
		using var content = new MemoryStream(new byte[] { 1, 2, 3 });
		using var cts = new CancellationTokenSource();

		// Act
		var url = await sut.UploadAsync(content, "file.txt", "text/plain", cts.Token);

		// Assert — normal completion when token is not cancelled
		url.Should().StartWith("/uploads/");
	}

	[Fact]
	public async Task UploadAsync_EachCall_Should_ProduceDifferentUrl()
	{
		// Arrange — two uploads of identical content must get distinct URLs (GUID prefix)
		var sut = CreateSut();

		// Act
		var url1 = await sut.UploadAsync(new MemoryStream(new byte[] { 1 }), "a.txt", "text/plain");
		var url2 = await sut.UploadAsync(new MemoryStream(new byte[] { 1 }), "a.txt", "text/plain");

		// Assert
		url1.Should().NotBe(url2);
	}

	#endregion

	// -----------------------------------------------------------------------
	// DownloadAsync
	// -----------------------------------------------------------------------

	#region DownloadAsync

	[Fact]
	public async Task DownloadAsync_Should_ReturnReadableStream_When_FileExists()
	{
		// Arrange
		var sut = CreateSut();
		using var uploadContent = new MemoryStream(new byte[] { 1, 2, 3 });
		var url = await sut.UploadAsync(uploadContent, "file.txt", "text/plain");

		// Act
		await using var stream = await sut.DownloadAsync(url);

		// Assert
		stream.Should().NotBeNull();
		stream.CanRead.Should().BeTrue();
	}

	[Fact]
	public async Task DownloadAsync_Should_ReturnOriginalBytes_When_FileExists()
	{
		// Arrange
		var sut = CreateSut();
		var original = new byte[] { 0x11, 0x22, 0x33, 0x44 };
		using var uploadContent = new MemoryStream(original);
		var url = await sut.UploadAsync(uploadContent, "data.bin", "application/octet-stream");

		// Act
		await using var downloadStream = await sut.DownloadAsync(url);
		var buffer = new MemoryStream();
		await downloadStream.CopyToAsync(buffer);

		// Assert
		buffer.ToArray().Should().Equal(original);
	}

	[Fact]
	public async Task DownloadAsync_Should_ThrowFileNotFoundException_When_FileDoesNotExist()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var act = () => sut.DownloadAsync("/uploads/ghost-file.txt");

		// Assert
		await act.Should().ThrowAsync<FileNotFoundException>();
	}

	[Fact]
	public async Task DownloadAsync_Should_RethrowException_And_NotSwallow()
	{
		// Arrange — use the same sut (file simply doesn't exist)
		var sut = CreateSut();

		// Act
		var act = () => sut.DownloadAsync("/uploads/never-created.bin");

		// Assert — any exception propagates out; service must not return null silently
		await act.Should().ThrowAsync<Exception>();
	}

	#endregion

	// -----------------------------------------------------------------------
	// DeleteAsync
	// -----------------------------------------------------------------------

	#region DeleteAsync

	[Fact]
	public async Task DeleteAsync_Should_RemovePhysicalFile_When_FileExists()
	{
		// Arrange
		var sut = CreateSut();
		using var content = new MemoryStream(new byte[] { 9, 8, 7 });
		var url = await sut.UploadAsync(content, "delete-me.txt", "text/plain");
		File.Exists(PhysicalPath(url)).Should().BeTrue("pre-condition: file must exist before delete");

		// Act
		await sut.DeleteAsync(url);

		// Assert
		File.Exists(PhysicalPath(url)).Should().BeFalse();
	}

	[Fact]
	public async Task DeleteAsync_Should_NotThrow_When_FileDoesNotExist()
	{
		// Arrange
		var sut = CreateSut();

		// Act
		var act = () => sut.DeleteAsync("/uploads/i-was-never-here.txt");

		// Assert — must be a no-op
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task DeleteAsync_Should_NotRemoveOtherFiles_When_DeletingSpecificFile()
	{
		// Arrange
		var sut = CreateSut();
		var url1 = await sut.UploadAsync(new MemoryStream(new byte[] { 1 }), "keep.txt", "text/plain");
		var url2 = await sut.UploadAsync(new MemoryStream(new byte[] { 2 }), "remove.txt", "text/plain");

		// Act — delete only the second file
		await sut.DeleteAsync(url2);

		// Assert
		File.Exists(PhysicalPath(url1)).Should().BeTrue("kept file must survive");
		File.Exists(PhysicalPath(url2)).Should().BeFalse("deleted file must be gone");
	}

	#endregion

	// -----------------------------------------------------------------------
	// GenerateThumbnailAsync
	// -----------------------------------------------------------------------

	#region GenerateThumbnailAsync

	[Fact]
	public async Task GenerateThumbnailAsync_Should_ReturnUrlUnderThumbnailsPath_When_ImageIsValid()
	{
		// Arrange
		var sut = CreateSut();
		await using var jpeg = await CreateJpegStreamAsync();
		var imageUrl = await sut.UploadAsync(jpeg, "image.jpg", "image/jpeg");

		// Act
		var thumbUrl = await sut.GenerateThumbnailAsync(imageUrl);

		// Assert
		thumbUrl.Should().NotBeNull();
		thumbUrl.Should().StartWith("/uploads/thumbnails/");
	}

	[Fact]
	public async Task GenerateThumbnailAsync_Should_ReturnUrlContainingThumbnailSuffix()
	{
		// Arrange
		var sut = CreateSut();
		await using var jpeg = await CreateJpegStreamAsync();
		var imageUrl = await sut.UploadAsync(jpeg, "photo.jpg", "image/jpeg");

		// Act
		var thumbUrl = await sut.GenerateThumbnailAsync(imageUrl);

		// Assert
		thumbUrl.Should().Contain("_thumbnail.jpg");
	}

	[Fact]
	public async Task GenerateThumbnailAsync_Should_CreateThumbnailFileOnDisk()
	{
		// Arrange
		var sut = CreateSut();
		await using var jpeg = await CreateJpegStreamAsync();
		var imageUrl = await sut.UploadAsync(jpeg, "photo.jpg", "image/jpeg");

		// Act
		var thumbUrl = await sut.GenerateThumbnailAsync(imageUrl);

		// Assert
		thumbUrl.Should().NotBeNull();
		File.Exists(PhysicalPath(thumbUrl!)).Should().BeTrue();
	}

	[Fact]
	public async Task GenerateThumbnailAsync_Should_ProduceThumbnailWithinMaxDimensions()
	{
		// Arrange — larger source so resize actually has work to do
		var sut = CreateSut();
		await using var jpeg = await CreateJpegStreamAsync(width: 400, height: 300);
		var imageUrl = await sut.UploadAsync(jpeg, "big.jpg", "image/jpeg");

		// Act
		var thumbUrl = await sut.GenerateThumbnailAsync(imageUrl);

		// Assert — ImageSharp ResizeMode.Max keeps aspect ratio within 200×200
		thumbUrl.Should().NotBeNull();
		using var thumb = Image.Load(PhysicalPath(thumbUrl!));
		thumb.Width.Should().BeLessThanOrEqualTo(200);
		thumb.Height.Should().BeLessThanOrEqualTo(200);
	}

	[Fact]
	public async Task GenerateThumbnailAsync_Should_ReturnNull_When_SourceFileIsNotAnImage()
	{
		// Arrange — upload plaintext so ImageSharp.LoadAsync throws
		var sut = CreateSut();
		using var notAnImage = new MemoryStream("this is not an image"u8.ToArray());
		var url = await sut.UploadAsync(notAnImage, "note.txt", "text/plain");

		// Act
		var thumbUrl = await sut.GenerateThumbnailAsync(url);

		// Assert — service catches the decode error and returns null
		thumbUrl.Should().BeNull();
	}

	[Fact]
	public async Task GenerateThumbnailAsync_Should_ReturnNull_When_SourceFileDoesNotExist()
	{
		// Arrange
		var sut = CreateSut();

		// Act — DownloadAsync throws FileNotFoundException; service must catch it
		var thumbUrl = await sut.GenerateThumbnailAsync("/uploads/ghost-image.jpg");

		// Assert
		thumbUrl.Should().BeNull();
	}

	#endregion
}
