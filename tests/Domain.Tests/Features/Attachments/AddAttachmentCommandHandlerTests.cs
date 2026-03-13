// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddAttachmentCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Attachments;

/// <summary>
///   Unit tests for AddAttachmentCommandHandler.
/// </summary>
public sealed class AddAttachmentCommandHandlerTests
{
	private readonly IRepository<Attachment> _repository;
	private readonly IFileStorageService _fileStorageService;
	private readonly ILogger<AddAttachmentCommandHandler> _logger;
	private readonly AddAttachmentCommandHandler _sut;

	public AddAttachmentCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Attachment>>();
		_fileStorageService = Substitute.For<IFileStorageService>();
		_logger = Substitute.For<ILogger<AddAttachmentCommandHandler>>();
		_sut = new AddAttachmentCommandHandler(_repository, _fileStorageService, _logger);
	}

	[Fact]
	public async Task AddAttachment_WithValidFile_ReturnsSuccess()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");
		var fileStream = new MemoryStream([0x01, 0x02, 0x03]);
		var blobUrl = "https://storage.example.com/attachments/test-file.pdf";

		var command = new AddAttachmentCommand(
			issueId.ToString(),
			fileStream,
			"test-file.pdf",
			"application/pdf",
			1024,
			uploader);

		_fileStorageService.UploadAsync(
				Arg.Any<Stream>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>())
			.Returns(blobUrl);

		_repository.AddAsync(Arg.Any<Attachment>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var attachment = callInfo.Arg<Attachment>();
				return Result.Ok(attachment);
			});

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.FileName.Should().Be("test-file.pdf");
		result.Value.ContentType.Should().Be("application/pdf");
		result.Value.FileSize.Should().Be(1024);
		result.Value.BlobUrl.Should().Be(blobUrl);
	}

	[Fact]
	public async Task AddAttachment_StoresFileViaIFileStorageService()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");
		var fileStream = new MemoryStream([0x01, 0x02, 0x03]);
		var blobUrl = "https://storage.example.com/attachments/document.pdf";

		var command = new AddAttachmentCommand(
			issueId.ToString(),
			fileStream,
			"document.pdf",
			"application/pdf",
			2048,
			uploader);

		_fileStorageService.UploadAsync(
				Arg.Any<Stream>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>())
			.Returns(blobUrl);

		_repository.AddAsync(Arg.Any<Attachment>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var attachment = callInfo.Arg<Attachment>();
				return Result.Ok(attachment);
			});

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _fileStorageService.Received(1).UploadAsync(
			fileStream,
			"document.pdf",
			"application/pdf",
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task AddAttachment_CreatesAttachmentRecord()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");
		var fileStream = new MemoryStream([0x01, 0x02, 0x03]);
		var blobUrl = "https://storage.example.com/attachments/report.pdf";

		var command = new AddAttachmentCommand(
			issueId.ToString(),
			fileStream,
			"report.pdf",
			"application/pdf",
			4096,
			uploader);

		_fileStorageService.UploadAsync(
				Arg.Any<Stream>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>())
			.Returns(blobUrl);

		_repository.AddAsync(Arg.Any<Attachment>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var attachment = callInfo.Arg<Attachment>();
				return Result.Ok(attachment);
			});

		// Act
		await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _repository.Received(1).AddAsync(
			Arg.Is<Attachment>(a =>
				a.FileName == "report.pdf" &&
				a.ContentType == "application/pdf" &&
				a.FileSize == 4096 &&
				a.BlobUrl == blobUrl &&
				a.UploadedBy == uploader),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task AddAttachment_WithImage_GeneratesThumbnail()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");
		var fileStream = new MemoryStream([0x01, 0x02, 0x03]);
		var blobUrl = "https://storage.example.com/attachments/image.png";
		var thumbnailUrl = "https://storage.example.com/attachments/image-thumb.png";

		var command = new AddAttachmentCommand(
			issueId.ToString(),
			fileStream,
			"image.png",
			"image/png", // Image content type
			8192,
			uploader);

		_fileStorageService.UploadAsync(
				Arg.Any<Stream>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>())
			.Returns(blobUrl);

		_fileStorageService.GenerateThumbnailAsync(blobUrl, Arg.Any<CancellationToken>())
			.Returns(thumbnailUrl);

		_repository.AddAsync(Arg.Any<Attachment>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var attachment = callInfo.Arg<Attachment>();
				return Result.Ok(attachment);
			});

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		await _fileStorageService.Received(1).GenerateThumbnailAsync(blobUrl, Arg.Any<CancellationToken>());
		result.Value!.ThumbnailUrl.Should().Be(thumbnailUrl);
	}

	[Fact]
	public async Task AddAttachment_WhenRepositorySaveFails_CleansUpUploadedFile()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");
		var fileStream = new MemoryStream([0x01, 0x02, 0x03]);
		var blobUrl = "https://storage.example.com/attachments/test.pdf";

		var command = new AddAttachmentCommand(
			issueId.ToString(),
			fileStream,
			"test.pdf",
			"application/pdf",
			1024,
			uploader);

		_fileStorageService.UploadAsync(
				Arg.Any<Stream>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CancellationToken>())
			.Returns(blobUrl);

		_repository.AddAsync(Arg.Any<Attachment>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Attachment>("Database error"));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		await _fileStorageService.Received(1).DeleteAsync(blobUrl, Arg.Any<CancellationToken>());
	}
}
