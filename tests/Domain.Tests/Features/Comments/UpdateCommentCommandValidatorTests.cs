// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateCommentCommandValidatorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Comments;

/// <summary>
///   Unit tests for UpdateCommentCommandValidator.
/// </summary>
public sealed class UpdateCommentCommandValidatorTests
{
	private readonly UpdateCommentCommandValidator _validator;

	public UpdateCommentCommandValidatorTests()
	{
		_validator = new UpdateCommentCommandValidator();
	}

	[Fact]
	public void CommentId_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			string.Empty, // Empty comment ID
			"Valid Title",
			"Valid Description",
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.CommentId)
			.WithErrorMessage("Comment ID is required");
	}

	[Fact]
	public void CommentId_WhenInvalidObjectId_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			"invalid-object-id", // Invalid ObjectId
			"Valid Title",
			"Valid Description",
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.CommentId)
			.WithErrorMessage("Comment ID must be a valid ObjectId");
	}

	[Fact]
	public void Content_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"Valid Title",
			string.Empty, // Empty description (content)
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Description)
			.WithErrorMessage("Description is required");
	}

	[Fact]
	public void Title_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			string.Empty, // Empty title
			"Valid Description",
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title)
			.WithErrorMessage("Title is required");
	}

	[Fact]
	public void Title_WhenTooShort_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"AB", // Too short (less than 3 characters)
			"Valid Description",
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title)
			.WithErrorMessage("Title must be at least 3 characters");
	}

	[Fact]
	public void Title_WhenTooLong_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			new string('A', 201), // Too long (more than 200 characters)
			"Valid Description",
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title)
			.WithErrorMessage("Title must not exceed 200 characters");
	}

	[Fact]
	public void Description_WhenTooShort_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"Valid Title",
			"AB", // Too short (less than 3 characters)
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Description)
			.WithErrorMessage("Description must be at least 3 characters");
	}

	[Fact]
	public void Description_WhenTooLong_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"Valid Title",
			new string('A', 5001), // Too long (more than 5000 characters)
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Description)
			.WithErrorMessage("Description must not exceed 5000 characters");
	}

	[Fact]
	public void RequestingUserId_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"Valid Title",
			"Valid Description",
			string.Empty); // Empty requesting user ID

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.RequestingUserId)
			.WithErrorMessage("Requesting user ID is required");
	}

	[Fact]
	public void ValidCommand_ShouldNotHaveErrors()
	{
		// Arrange
		var command = new UpdateCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"Valid Title",
			"Valid Description",
			"user-123");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveAnyValidationErrors();
	}
}
