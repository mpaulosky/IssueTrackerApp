// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteAttachmentCommandValidatorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Attachments;

/// <summary>
///   Unit tests for DeleteAttachmentCommandValidator.
/// </summary>
public sealed class DeleteAttachmentCommandValidatorTests
{
	private readonly DeleteAttachmentCommandValidator _validator;

	public DeleteAttachmentCommandValidatorTests()
	{
		_validator = new DeleteAttachmentCommandValidator();
	}

	[Fact]
	public void AttachmentId_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new DeleteAttachmentCommand(
			string.Empty, // Empty attachment ID
			"user-123",
			false);

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.AttachmentId)
			.WithErrorMessage("Attachment ID is required");
	}

	[Fact]
	public void AttachmentId_WhenInvalidObjectId_ShouldHaveError()
	{
		// Arrange
		var command = new DeleteAttachmentCommand(
			"invalid-object-id", // Invalid ObjectId
			"user-123",
			false);

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.AttachmentId)
			.WithErrorMessage("Attachment ID must be a valid ObjectId");
	}

	[Fact]
	public void UserId_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new DeleteAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			string.Empty, // Empty user ID
			false);

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.UserId)
			.WithErrorMessage("User ID is required");
	}

	[Fact]
	public void ValidCommand_ShouldNotHaveErrors()
	{
		// Arrange
		var command = new DeleteAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			"user-123",
			false);

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void ValidCommand_WithAdminFlag_ShouldNotHaveErrors()
	{
		// Arrange
		var command = new DeleteAttachmentCommand(
			ObjectId.GenerateNewId().ToString(),
			"admin-123",
			true);

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveAnyValidationErrors();
	}
}
