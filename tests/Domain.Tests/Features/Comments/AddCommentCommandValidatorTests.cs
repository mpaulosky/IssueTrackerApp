// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddCommentCommandValidatorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Comments;

/// <summary>
///   Unit tests for AddCommentCommandValidator.
/// </summary>
public sealed class AddCommentCommandValidatorTests
{
	private readonly AddCommentCommandValidator _validator;

	public AddCommentCommandValidatorTests()
	{
		_validator = new AddCommentCommandValidator();
	}

	[Fact]
	public void Content_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new AddCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"Valid Title",
			string.Empty, // Empty description (content)
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Description)
			.WithErrorMessage("Description is required");
	}

	[Fact]
	public void IssueId_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new AddCommentCommand(
			string.Empty, // Empty issue ID
			"Valid Title",
			"Valid Description",
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
		var command = new AddCommentCommand(
			"invalid-object-id", // Invalid ObjectId
			"Valid Title",
			"Valid Description",
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.IssueId)
			.WithErrorMessage("Issue ID must be a valid ObjectId");
	}

	[Fact]
	public void Title_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new AddCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			string.Empty, // Empty title
			"Valid Description",
			new UserDto("user-123", "Test User", "test@example.com"));

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
		var command = new AddCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"AB", // Too short (less than 3 characters)
			"Valid Description",
			new UserDto("user-123", "Test User", "test@example.com"));

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
		var command = new AddCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			new string('A', 201), // Too long (more than 200 characters)
			"Valid Description",
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title)
			.WithErrorMessage("Title must not exceed 200 characters");
	}

	[Fact]
	public void Author_WhenNull_ShouldHaveError()
	{
		// Arrange
		var command = new AddCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"Valid Title",
			"Valid Description",
			null!); // Null author

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Author)
			.WithErrorMessage("Author is required");
	}

	[Fact]
	public void ValidCommand_ShouldNotHaveErrors()
	{
		// Arrange
		var command = new AddCommentCommand(
			ObjectId.GenerateNewId().ToString(),
			"Valid Title",
			"Valid Description",
			new UserDto("user-123", "Test User", "test@example.com"));

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveAnyValidationErrors();
	}
}
