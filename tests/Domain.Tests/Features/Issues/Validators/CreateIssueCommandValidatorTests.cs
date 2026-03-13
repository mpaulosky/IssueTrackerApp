// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateIssueCommandValidatorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Issues.Commands;
using Domain.Features.Issues.Validators;

using FluentValidation.TestHelper;

namespace Domain.Tests.Features.Issues.Validators;

/// <summary>
///   Unit tests for CreateIssueCommandValidator.
/// </summary>
public class CreateIssueCommandValidatorTests
{
	private readonly CreateIssueCommandValidator _validator;

	public CreateIssueCommandValidatorTests()
	{
		_validator = new CreateIssueCommandValidator();
	}

	private static CategoryDto CreateValidCategory()
	{
		return new CategoryDto(
			ObjectId.GenerateNewId(),
			"Bug",
			"Bug category description",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	private static UserDto CreateValidAuthor()
	{
		return new UserDto("user-123", "John Doe", "john@example.com");
	}

	private static CreateIssueCommand CreateValidCommand()
	{
		return new CreateIssueCommand(
			"Valid Title Here",
			"This is a valid description with enough characters",
			CreateValidCategory(),
			CreateValidAuthor());
	}

	#region Title Validation Tests

	[Fact]
	public void Title_WhenEmpty_ShouldHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand() with { Title = "" };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title)
			.WithErrorMessage("Title is required");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Title_WhenEmptyOrWhitespace_ShouldHaveValidationError(string title)
	{
		// Arrange
		var command = CreateValidCommand() with { Title = title };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title);
	}

	[Theory]
	[InlineData("ab")]
	[InlineData("abc")]
	[InlineData("abcd")]
	public void Title_WhenTooShort_ShouldHaveValidationError(string title)
	{
		// Arrange
		var command = CreateValidCommand() with { Title = title };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title)
			.WithErrorMessage("Title must be at least 5 characters");
	}

	[Fact]
	public void Title_WhenTooLong_ShouldHaveValidationError()
	{
		// Arrange
		var longTitle = new string('a', 201);
		var command = CreateValidCommand() with { Title = longTitle };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title)
			.WithErrorMessage("Title must not exceed 200 characters");
	}

	[Theory]
	[InlineData("Valid Title")]
	[InlineData("This is a valid issue title")]
	[InlineData("Bug: Something is broken")]
	public void Title_WhenValid_ShouldNotHaveValidationError(string title)
	{
		// Arrange
		var command = CreateValidCommand() with { Title = title };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Title);
	}

	[Fact]
	public void Title_WhenExactlyMinLength_ShouldNotHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand() with { Title = "12345" }; // 5 characters

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Title);
	}

	[Fact]
	public void Title_WhenExactlyMaxLength_ShouldNotHaveValidationError()
	{
		// Arrange
		var maxLengthTitle = new string('a', 200);
		var command = CreateValidCommand() with { Title = maxLengthTitle };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Title);
	}

	#endregion

	#region Description Validation Tests

	[Fact]
	public void Description_WhenEmpty_ShouldHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand() with { Description = "" };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Description)
			.WithErrorMessage("Description is required");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Description_WhenEmptyOrWhitespace_ShouldHaveValidationError(string description)
	{
		// Arrange
		var command = CreateValidCommand() with { Description = description };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Description);
	}

	[Theory]
	[InlineData("short")]
	[InlineData("too small")]
	public void Description_WhenTooShort_ShouldHaveValidationError(string description)
	{
		// Arrange
		var command = CreateValidCommand() with { Description = description };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Description)
			.WithErrorMessage("Description must be at least 10 characters");
	}

	[Fact]
	public void Description_WhenTooLong_ShouldHaveValidationError()
	{
		// Arrange
		var longDescription = new string('a', 5001);
		var command = CreateValidCommand() with { Description = longDescription };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Description)
			.WithErrorMessage("Description must not exceed 5000 characters");
	}

	[Fact]
	public void Description_WhenValid_ShouldNotHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand() with { Description = "This is a valid description for the issue" };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Description);
	}

	[Fact]
	public void Description_WhenExactlyMinLength_ShouldNotHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand() with { Description = "1234567890" }; // 10 characters

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Description);
	}

	[Fact]
	public void Description_WhenExactlyMaxLength_ShouldNotHaveValidationError()
	{
		// Arrange
		var maxLengthDescription = new string('a', 5000);
		var command = CreateValidCommand() with { Description = maxLengthDescription };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Description);
	}

	#endregion

	#region Category Validation Tests

	[Fact]
	public void Category_WhenNull_ShouldHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand() with { Category = null! };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Category)
			.WithErrorMessage("Category is required");
	}

	[Fact]
	public void CategoryName_WhenEmpty_ShouldHaveValidationError()
	{
		// Arrange
		var emptyCategory = new CategoryDto(
			ObjectId.GenerateNewId(),
			"",
			"Description",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
		var command = CreateValidCommand() with { Category = emptyCategory };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Category.CategoryName)
			.WithErrorMessage("Category name is required");
	}

	[Fact]
	public void Category_WhenValid_ShouldNotHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand();

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Category);
		result.ShouldNotHaveValidationErrorFor(x => x.Category.CategoryName);
	}

	#endregion

	#region Author Validation Tests

	[Fact]
	public void Author_WhenNull_ShouldHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand() with { Author = null! };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Author)
			.WithErrorMessage("Author is required");
	}

	[Fact]
	public void AuthorId_WhenEmpty_ShouldHaveValidationError()
	{
		// Arrange
		var emptyAuthor = new UserDto("", "John Doe", "john@example.com");
		var command = CreateValidCommand() with { Author = emptyAuthor };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Author.Id)
			.WithErrorMessage("Author ID is required");
	}

	[Fact]
	public void AuthorName_WhenEmpty_ShouldHaveValidationError()
	{
		// Arrange
		var emptyNameAuthor = new UserDto("user-123", "", "john@example.com");
		var command = CreateValidCommand() with { Author = emptyNameAuthor };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Author.Name)
			.WithErrorMessage("Author name is required");
	}

	[Fact]
	public void Author_WhenValid_ShouldNotHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand();

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Author);
		result.ShouldNotHaveValidationErrorFor(x => x.Author.Id);
		result.ShouldNotHaveValidationErrorFor(x => x.Author.Name);
	}

	#endregion

	#region Full Command Validation Tests

	[Fact]
	public void ValidCommand_ShouldNotHaveAnyValidationErrors()
	{
		// Arrange
		var command = CreateValidCommand();

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void InvalidCommand_WithMultipleErrors_ShouldHaveAllValidationErrors()
	{
		// Arrange
		var command = new CreateIssueCommand("", "", null!, null!);

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Title);
		result.ShouldHaveValidationErrorFor(x => x.Description);
		result.ShouldHaveValidationErrorFor(x => x.Category);
		result.ShouldHaveValidationErrorFor(x => x.Author);
	}

	#endregion
}
