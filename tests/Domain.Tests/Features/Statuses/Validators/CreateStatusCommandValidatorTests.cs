// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateStatusCommandValidatorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Statuses.Commands;
using Domain.Features.Statuses.Validators;

namespace Domain.Tests.Features.Statuses.Validators;

/// <summary>
///   Unit tests for CreateStatusCommandValidator.
/// </summary>
public class CreateStatusCommandValidatorTests
{
	private readonly CreateStatusCommandValidator _validator;

	public CreateStatusCommandValidatorTests()
	{
		_validator = new CreateStatusCommandValidator();
	}

	/// <summary>
	///   Verifies that an empty status name produces a validation error.
	/// </summary>
	[Fact]
	public void StatusName_WhenEmpty_ShouldHaveError()
	{
		// Arrange
		var command = new CreateStatusCommand(string.Empty, "Valid Description");

		// Act
		var result = _validator.TestValidate(command);

		// Assert
		result.ShouldHaveValidationErrorFor(x => x.StatusName)
			.WithErrorMessage("Status name is required");
	}
}
