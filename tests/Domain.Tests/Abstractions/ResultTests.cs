// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ResultTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;

namespace Domain.Tests.Abstractions;

/// <summary>
///   Unit tests for the Result and Result{T} classes.
/// </summary>
public sealed class ResultTests
{
	[Fact]
	public void Success_ReturnsIsSuccessTrue()
	{
		// Act
		var result = Result.Ok();

		// Assert
		result.Success.Should().BeTrue();
		result.Failure.Should().BeFalse();
		result.Error.Should().BeNull();
		result.ErrorCode.Should().Be(ResultErrorCode.None);
	}

	[Fact]
	public void Success_WithValue_ContainsValue()
	{
		// Arrange
		const string expectedValue = "test value";

		// Act
		var result = Result.Ok(expectedValue);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be(expectedValue);
		result.Error.Should().BeNull();
		result.ErrorCode.Should().Be(ResultErrorCode.None);
	}

	[Fact]
	public void Failure_ReturnsIsSuccessFalse()
	{
		// Arrange
		const string errorMessage = "Something went wrong";

		// Act
		var result = Result.Fail(errorMessage);

		// Assert
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.Error.Should().Be(errorMessage);
	}

	[Fact]
	public void Failure_ContainsErrorCode()
	{
		// Arrange
		const string errorMessage = "Not found";

		// Act
		var result = Result<string>.Fail(errorMessage, ResultErrorCode.NotFound);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be(errorMessage);
	}

	[Fact]
	public void Failure_ContainsErrorMessage()
	{
		// Arrange
		const string errorMessage = "Validation failed";

		// Act
		var result = Result<int>.Fail(errorMessage, ResultErrorCode.Validation);

		// Assert
		result.Error.Should().Be(errorMessage);
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Value.Should().Be(default);
	}

	[Fact]
	public void ImplicitConversion_FromValue_CreatesSuccess()
	{
		// Arrange
		const string value = "implicit value";

		// Act
		Result<string> result = value;

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be(value);
	}

	[Fact]
	public void NotFound_ReturnsNotFoundErrorCode()
	{
		// Arrange
		const string errorMessage = "Item not found";

		// Act
		var result = Result<string>.Fail(errorMessage, ResultErrorCode.NotFound);

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Be(errorMessage);
		result.Success.Should().BeFalse();
	}

	[Fact]
	public void ValidationError_ReturnsValidationErrorCode()
	{
		// Arrange
		const string errorMessage = "Invalid input data";

		// Act
		var result = Result<string>.Fail(errorMessage, ResultErrorCode.Validation);

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Error.Should().Be(errorMessage);
	}

	[Fact]
	public void Map_WhenSuccess_TransformsValue()
	{
		// Arrange
		var result = Result.Ok(5);

		// Act - Manual map since Result<T> doesn't have Map method
		var mappedResult = result.Success
			? Result.Ok(result.Value * 2)
			: Result.Fail<int>(result.Error ?? "Unknown error", result.ErrorCode);

		// Assert
		mappedResult.Success.Should().BeTrue();
		mappedResult.Value.Should().Be(10);
	}

	[Fact]
	public void Map_WhenFailure_PropagatesError()
	{
		// Arrange
		var result = Result<int>.Fail("Original error", ResultErrorCode.Validation);

		// Act - Manual map since Result<T> doesn't have Map method
		var mappedResult = result.Success
			? Result.Ok(result.Value * 2)
			: Result.Fail<int>(result.Error ?? "Unknown error", result.ErrorCode);

		// Assert
		mappedResult.Success.Should().BeFalse();
		mappedResult.Error.Should().Be("Original error");
		mappedResult.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public void Fail_WithDetails_ContainsDetails()
	{
		// Arrange
		var details = new { Version = 2, UpdatedBy = "user123" };

		// Act
		var result = Result.Fail("Concurrency conflict", ResultErrorCode.Concurrency, details);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Concurrency);
		result.Details.Should().BeEquivalentTo(details);
	}

	[Fact]
	public void GenericFail_WithDetails_ContainsDetails()
	{
		// Arrange
		var details = new { Field = "Email", Reason = "Already exists" };

		// Act
		var result = Result<string>.Fail("Conflict error", ResultErrorCode.Conflict, details);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Conflict);
		result.Details.Should().BeEquivalentTo(details);
	}

	[Fact]
	public void FromValue_WithNonNullValue_ReturnsSuccess()
	{
		// Arrange
		const string value = "test";

		// Act
		var result = Result.FromValue(value);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be(value);
	}

	[Fact]
	public void FromValue_WithNullValue_ReturnsFailure()
	{
		// Arrange
		string? value = null;

		// Act
		var result = Result.FromValue(value);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Provided value is null.");
	}

	[Fact]
	public void ImplicitConversion_ToValue_ReturnsValue()
	{
		// Arrange
		var result = Result.Ok("test value");

		// Act
		string? value = result;

		// Assert
		value.Should().Be("test value");
	}

	[Fact]
	public void ImplicitConversion_NullResult_ReturnsDefault()
	{
		// Arrange
		Result<int>? result = null;

		// Act
		int value = result;

		// Assert
		value.Should().Be(0);
	}
}
