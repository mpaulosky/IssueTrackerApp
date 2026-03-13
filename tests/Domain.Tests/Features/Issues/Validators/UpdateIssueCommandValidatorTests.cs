// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateIssueCommandValidatorTests.cs
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
///   Unit tests for UpdateIssueCommandValidator.
/// </summary>
public class UpdateIssueCommandValidatorTests
{
	private readonly UpdateIssueCommandValidator _validator;

	public UpdateIssueCommandValidatorTests()
	{
		_validator = new UpdateIssueCommandValidator();
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

	private static string CreateValidObjectId()
	{
		return ObjectId.GenerateNewId().ToString();
	}

	private static UpdateIssueCommand CreateValidCommand()
	{
		return new UpdateIssueCommand(
			CreateValidObjectId(),
			"Valid Title Here",
			"This is a valid description with enough characters",
			CreateValidCategory());
	}

	#region Id Validation Tests

	[Fact]
	public void IssueId_WhenEmpty_ShouldHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand() with { Id = "" };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Id)
			.WithErrorMessage("Issue ID is required");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void IssueId_WhenEmptyOrWhitespace_ShouldHaveValidationError(string id)
	{
		// Arrange
		var command = CreateValidCommand() with { Id = id };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Id);
	}

	[Theory]
	[InlineData("invalid-id")]
	[InlineData("123")]
	[InlineData("not-a-valid-objectid")]
	public void IssueId_WhenInvalidObjectId_ShouldHaveValidationError(string id)
	{
		// Arrange
		var command = CreateValidCommand() with { Id = id };

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Id)
			.WithErrorMessage("Issue ID must be a valid ObjectId");
	}

	[Fact]
	public void IssueId_WhenValidObjectId_ShouldNotHaveValidationError()
	{
		// Arrange
		var command = CreateValidCommand();

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.Id);
	}

	#endregion

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

	#region Full Command Validation Tests

	[Fact]
	public void AllFieldsValid_ShouldPassValidation()
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
		var command = new UpdateIssueCommand("", "", "", null!);

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.Id);
		result.ShouldHaveValidationErrorFor(x => x.Title);
		result.ShouldHaveValidationErrorFor(x => x.Description);
		result.ShouldHaveValidationErrorFor(x => x.Category);
	}

	[Fact]
	public void ValidCommand_WithValidObjectId_ShouldNotHaveAnyValidationErrors()
	{
		// Arrange
		var objectId = ObjectId.GenerateNewId().ToString();
		var command = new UpdateIssueCommand(
			objectId,
			"Valid Issue Title",
			"This is a valid description for the issue update",
			CreateValidCategory());

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveAnyValidationErrors();
	}

	#endregion
}
