// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddAttachmentCommandValidatorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Attachments;

/// <summary>
///   Unit tests for AddAttachmentCommandValidator.
/// </summary>
public sealed class AddAttachmentCommandValidatorTests
{
	private readonly AddAttachmentCommandValidator _validator;

	public AddAttachmentCommandValidatorTests()
	{
		_validator = new AddAttachmentCommandValidator();
	}

	[Fact]
	public void FileName_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			new MemoryStream([0x01]),
			string.Empty, // Empty file name
			"application/pdf",
			1024,
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.FileName)
			.WithErrorMessage("File name is required");
	}

	[Fact]
	public void FileSize_WhenZero_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			new MemoryStream([0x01]),
			"test.pdf",
			"application/pdf",
			0, // Zero file size
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.FileSize)
			.WithErrorMessage("File size must be greater than 0");
	}

	[Fact]
	public void ContentType_WhenInvalid_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			new MemoryStream([0x01]),
			"test.exe",
			"application/x-msdownload", // Invalid content type
			1024,
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.ContentType);
	}

	[Fact]
	public void ContentType_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			new MemoryStream([0x01]),
			"test.pdf",
			string.Empty, // Empty content type
			1024,
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.ContentType)
			.WithErrorMessage("Content type is required");
	}

	[Fact]
	public void IssueId_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			string.Empty, // Empty issue ID
			new MemoryStream([0x01]),
			"test.pdf",
			"application/pdf",
			1024,
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.IssueId)
			.WithErrorMessage("Issue ID is required");
	}

	[Fact]
	public void IssueId_WhenInvalidObjectId_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			"invalid-object-id", // Invalid ObjectId
			new MemoryStream([0x01]),
			"test.pdf",
			"application/pdf",
			1024,
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.IssueId)
			.WithErrorMessage("Issue ID must be a valid ObjectId");
	}

	[Fact]
	public void FileSize_WhenExceedsMaximum_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			new MemoryStream([0x01]),
			"test.pdf",
			"application/pdf",
			FileValidationConstants.MAX_FILE_SIZE + 1, // Exceeds max
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.FileSize);
	}

	[Fact]
	public void FileContent_WhenNull_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			null!, // Null file content
			"test.pdf",
			"application/pdf",
			1024,
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.FileContent)
			.WithErrorMessage("File content is required");
	}

	[Fact]
	public void UploadedBy_WhenNull_ShouldHaveError()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			new MemoryStream([0x01]),
			"test.pdf",
			"application/pdf",
			1024,
			null!); // Null uploader

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.UploadedBy)
			.WithErrorMessage("Uploaded by user is required");
	}

	[Theory]
	[InlineData("image/jpeg")]
	[InlineData("image/png")]
	[InlineData("image/gif")]
	[InlineData("image/webp")]
	[InlineData("application/pdf")]
	[InlineData("text/plain")]
	[InlineData("text/markdown")]
	public void ContentType_WhenAllowed_ShouldNotHaveError(string contentType)
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			new MemoryStream([0x01]),
			"test.file",
			contentType,
			1024,
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
	}

	[Fact]
	public void ValidCommand_ShouldNotHaveErrors()
	{
		// Arrange
		var command = new AddAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			new MemoryStream([0x01, 0x02, 0x03]),
			"test-document.pdf",
			"application/pdf",
			1024,
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveAnyValidationErrors();
	}
}
