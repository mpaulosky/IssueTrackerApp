// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateCategoryCommandValidatorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Categories.Commands;
using Domain.Features.Categories.Validators;

namespace Domain.Tests.Features.Categories.Validators;

/// <summary>
///   Unit tests for CreateCategoryCommandValidator.
/// </summary>
public class CreateCategoryCommandValidatorTests
{
	private readonly CreateCategoryCommandValidator _validator;

	public CreateCategoryCommandValidatorTests()
	{
		_validator = new CreateCategoryCommandValidator();
	}

	/// <summary>
	///   Verifies that an empty category name produces a validation error.
	/// </summary>
	[Fact]
	public void CategoryName_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new CreateCategoryCommand(string.Empty, "Valid Description");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.CategoryName)
			.WithErrorMessage("Category name is required");
	}

	/// <summary>
	///   Verifies that a valid category name does not produce a validation error.
	/// </summary>
	[Fact]
	public void CategoryName_WhenValid_ShouldNotHaveError()
	{
		// Arrange
		var command = new CreateCategoryCommand("Valid Category Name", "Valid Description Here");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldNotHaveValidationErrorFor(x => x.CategoryName);
	}
}
