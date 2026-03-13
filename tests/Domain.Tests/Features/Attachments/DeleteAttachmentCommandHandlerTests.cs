// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteAttachmentCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Attachments;

/// <summary>
///   Unit tests for DeleteAttachmentCommandHandler.
/// </summary>
public sealed class DeleteAttachmentCommandHandlerTests
{
	private readonly IRepository<Attachment> _repository;
	private readonly IFileStorageService _fileStorageService;
	private readonly ILogger<DeleteAttachmentCommandHandler> _logger;
	private readonly DeleteAttachmentCommandHandler _sut;

	public DeleteAttachmentCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Attachment>>();
		_fileStorageService = Substitute.For<IFileStorageService>();
		_logger = Substitute.For<ILogger<DeleteAttachmentCommandHandler>>();
		_sut = new DeleteAttachmentCommandHandler(_repository, _fileStorageService, _logger);
	}

	[Fact]
	public async Task DeleteAttachment_WhenExists_RemovesFromStorageAndDatabase()
	{
		// Arrange
		var attachmentId = ObjectId.GenerateNewId();
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");
		var blobUrl = "https://storage.example.com/attachments/test.pdf";
		var thumbnailUrl = "https://storage.example.com/attachments/test-thumb.png";

		var existingAttachment = new Attachment
		{
			Id = attachmentId,
			IssueId = issueId,
			FileName = "test.pdf",
			ContentType = "application/pdf",
			FileSize = 1024,
			BlobUrl = blobUrl,
			ThumbnailUrl = thumbnailUrl,
			UploadedBy = uploader,
			UploadedAt = DateTime.UtcNow.AddHours(-1)
		};

		var command = new DeleteAttachmentCommand(
			attachmentId.ToString(),
			uploader.Id,
			false);

		_repository.GetByIdAsync(attachmentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingAttachment));

		_repository.DeleteAsync(attachmentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();

		// Verify storage deletion
		await _fileStorageService.Received(1).DeleteAsync(blobUrl, Arg.Any<CancellationToken>());
		await _fileStorageService.Received(1).DeleteAsync(thumbnailUrl, Arg.Any<CancellationToken>());

		// Verify database deletion
		await _repository.Received(1).DeleteAsync(attachmentId.ToString(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteAttachment_WhenNotFound_ReturnsError()
	{
		// Arrange
		var attachmentId = ObjectId.GenerateNewId();
		var command = new DeleteAttachmentCommand(
			attachmentId.ToString(),
			"user-123",
			false);

		_repository.GetByIdAsync(attachmentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Attachment>("Attachment not found", ResultErrorCode.NotFound));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Attachment not found");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);

		// Verify no deletion occurred
		await _fileStorageService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
		await _repository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteAttachment_WhenAdmin_CanDeleteOthersAttachment()
	{
		// Arrange
		var attachmentId = ObjectId.GenerateNewId();
		var issueId = ObjectId.GenerateNewId();
		var originalUploader = new UserDto("original-user-123", "Original User", "original@example.com");
		var blobUrl = "https://storage.example.com/attachments/document.pdf";

		var existingAttachment = new Attachment
		{
			Id = attachmentId,
			IssueId = issueId,
			FileName = "document.pdf",
			ContentType = "application/pdf",
			FileSize = 2048,
			BlobUrl = blobUrl,
			ThumbnailUrl = null,
			UploadedBy = originalUploader,
			UploadedAt = DateTime.UtcNow.AddHours(-2)
		};

		var command = new DeleteAttachmentCommand(
			attachmentId.ToString(),
			"admin-user-456", // Different user
			true); // IsAdmin = true

		_repository.GetByIdAsync(attachmentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingAttachment));

		_repository.DeleteAsync(attachmentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _repository.Received(1).DeleteAsync(attachmentId.ToString(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteAttachment_WhenNotOwnerAndNotAdmin_ReturnsError()
	{
		// Arrange
		var attachmentId = ObjectId.GenerateNewId();
		var issueId = ObjectId.GenerateNewId();
		var originalUploader = new UserDto("original-user-123", "Original User", "original@example.com");
		var blobUrl = "https://storage.example.com/attachments/file.pdf";

		var existingAttachment = new Attachment
		{
			Id = attachmentId,
			IssueId = issueId,
			FileName = "file.pdf",
			ContentType = "application/pdf",
			FileSize = 1024,
			BlobUrl = blobUrl,
			ThumbnailUrl = null,
			UploadedBy = originalUploader,
			UploadedAt = DateTime.UtcNow
		};

		var command = new DeleteAttachmentCommand(
			attachmentId.ToString(),
			"other-user-456", // Different user
			false); // Not admin

		_repository.GetByIdAsync(attachmentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingAttachment));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Unauthorized to delete this attachment");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);

		// Verify no deletion occurred
		await _fileStorageService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
		await _repository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteAttachment_WhenNoThumbnail_OnlyDeletesMainFile()
	{
		// Arrange
		var attachmentId = ObjectId.GenerateNewId();
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");
		var blobUrl = "https://storage.example.com/attachments/document.pdf";

		var existingAttachment = new Attachment
		{
			Id = attachmentId,
			IssueId = issueId,
			FileName = "document.pdf",
			ContentType = "application/pdf",
			FileSize = 1024,
			BlobUrl = blobUrl,
			ThumbnailUrl = null, // No thumbnail
			UploadedBy = uploader,
			UploadedAt = DateTime.UtcNow
		};

		var command = new DeleteAttachmentCommand(
			attachmentId.ToString(),
			uploader.Id,
			false);

		_repository.GetByIdAsync(attachmentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingAttachment));

		_repository.DeleteAsync(attachmentId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();

		// Verify only main blob was deleted
		await _fileStorageService.Received(1).DeleteAsync(blobUrl, Arg.Any<CancellationToken>());
	}
}
