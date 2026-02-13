// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Result.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Shared
// =======================================================

namespace Shared.Abstractions;

/// <summary>
///   Defines error codes for operation results.
/// </summary>
public enum ResultErrorCode
{
	/// <summary>
	///   No error occurred.
	/// </summary>
	None = 0,

	/// <summary>
	///   A concurrency conflict occurred.
	/// </summary>
	Concurrency = 1,

	/// <summary>
	///   The requested resource was not found.
	/// </summary>
	NotFound = 2,

	/// <summary>
	///   A validation error occurred.
	/// </summary>
	Validation = 3,

	/// <summary>
	///   A conflict occurred.
	/// </summary>
	Conflict = 4
}

/// <summary>
///   Represents the result of an operation, containing success status and optional error information.
/// </summary>
public class Result
{
	/// <summary>
	///   Initializes a new instance of the <see cref="Result" /> class.
	/// </summary>
	/// <param name="success"><see langword="true" /> if the operation succeeded; otherwise, <see langword="false" />.</param>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="errorCode">One of the enumeration values that specifies the error code.</param>
	/// <param name="details">Optional structured error details.</param>
	protected Result(bool success, string? errorMessage = null, ResultErrorCode errorCode = ResultErrorCode.None, object? details = null)
	{
		Success = success;
		Error = errorMessage;
		ErrorCode = errorCode;
		Details = details;
	}

	/// <summary>
	///   Gets a value that indicates whether the operation succeeded.
	/// </summary>
	/// <value>
	///   <see langword="true" /> if the operation succeeded; otherwise, <see langword="false" />.
	/// </value>
	public bool Success { get; }

	/// <summary>
	///   Gets a value that indicates whether the operation failed.
	/// </summary>
	/// <value>
	///   <see langword="true" /> if the operation failed; otherwise, <see langword="false" />.
	/// </value>
	public bool Failure => !Success;

	/// <summary>
	///   Gets the error message.
	/// </summary>
	/// <value>
	///   The error message, or <see langword="null" /> if the operation succeeded.
	/// </value>
	public string? Error { get; }

	/// <summary>
	///   Gets the error code.
	/// </summary>
	/// <value>
	///   One of the enumeration values that specifies the error code. The default is <see cref="ResultErrorCode.None" />.
	/// </value>
	public ResultErrorCode ErrorCode { get; }

	/// <summary>
	///   Gets optional structured error details.
	/// </summary>
	/// <value>
	///   Optional structured error details (e.g., server version on concurrency conflict), or <see langword="null" /> if no details are available.
	/// </value>
	public object? Details { get; }

	/// <summary>
	///   Creates a successful result.
	/// </summary>
	/// <returns>A <see cref="Result" /> representing a successful operation.</returns>
	public static Result Ok()
	{
		return new Result(true);
	}

	/// <summary>
	///   Creates a failed result with the specified error message.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	/// <returns>A <see cref="Result" /> representing a failed operation.</returns>
	public static Result Fail(string errorMessage)
	{
		return new Result(false, errorMessage, ResultErrorCode.None, null);
	}

	/// <summary>
	///   Creates a failed result with the specified error message and error code.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="code">One of the enumeration values that specifies the error code.</param>
	/// <returns>A <see cref="Result" /> representing a failed operation.</returns>
	public static Result Fail(string errorMessage, ResultErrorCode code)
	{
		return new Result(false, errorMessage, code, null);
	}

	/// <summary>
	///   Creates a failed result with the specified error message, error code, and details.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="code">One of the enumeration values that specifies the error code.</param>
	/// <param name="details">Optional structured error details.</param>
	/// <returns>A <see cref="Result" /> representing a failed operation.</returns>
	public static Result Fail(string errorMessage, ResultErrorCode code, object? details)
	{
		return new Result(false, errorMessage, code, details);
	}

	/// <summary>
	///   Creates a successful result with the specified value.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value.</param>
	/// <returns>A <see cref="Result{T}" /> representing a successful operation with a value.</returns>
	public static Result<T> Ok<T>(T value)
	{
		return new Result<T>(value, true);
	}

	/// <summary>
	///   Creates a failed result with the specified error message.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="errorMessage">The error message.</param>
	/// <returns>A <see cref="Result{T}" /> representing a failed operation.</returns>
	public static Result<T> Fail<T>(string errorMessage)
	{
		return new Result<T>(default, false, errorMessage);
	}

	/// <summary>
	///   Creates a failed result with the specified error message and error code.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="code">One of the enumeration values that specifies the error code.</param>
	/// <returns>A <see cref="Result{T}" /> representing a failed operation.</returns>
	public static Result<T> Fail<T>(string errorMessage, ResultErrorCode code)
	{
		return new Result<T>(default, false, errorMessage, code);
	}

	/// <summary>
	///   Creates a failed result with the specified error message, error code, and details.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="code">One of the enumeration values that specifies the error code.</param>
	/// <param name="details">Optional structured error details.</param>
	/// <returns>A <see cref="Result{T}" /> representing a failed operation.</returns>
	public static Result<T> Fail<T>(string errorMessage, ResultErrorCode code, object? details)
	{
		return new Result<T>(default, false, errorMessage, code, details);
	}

	/// <summary>
	///   Creates a result from the specified value.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value.</param>
	/// <returns>
	///   A successful <see cref="Result{T}" /> if <paramref name="value" /> is not <see langword="null" />;
	///   otherwise, a failed <see cref="Result{T}" />.
	/// </returns>
	public static Result<T> FromValue<T>(T? value)
	{
		return value is not null ? Ok(value) : Result<T>.Fail("Provided value is null.");
	}

}

/// <summary>
///   Represents the result of an operation with a typed value, containing success status and optional error information.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class Result<T> : Result
{
	/// <summary>
	///   Initializes a new instance of the <see cref="Result{T}" /> class.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="success"><see langword="true" /> if the operation succeeded; otherwise, <see langword="false" />.</param>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="errorCode">One of the enumeration values that specifies the error code.</param>
	/// <param name="details">Optional structured error details.</param>
	internal Result(T? value, bool success, string? errorMessage = null, ResultErrorCode errorCode = ResultErrorCode.None, object? details = null)
		: base(success, errorMessage, errorCode, details)
	{
		Value = value;
	}

	/// <summary>
	///   Gets the result value.
	/// </summary>
	/// <value>
	///   The result value, or the default value for <typeparamref name="T" /> if the operation failed.
	/// </value>
	public T? Value { get; }

	/// <summary>
	///   Creates a successful result with the specified value.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>A <see cref="Result{T}" /> representing a successful operation with a value.</returns>
	private static Result<T> Ok(T? value)
	{
		return new Result<T>(value, true);
	}

	/// <summary>
	///   Creates a failed result with the specified error message.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	/// <returns>A <see cref="Result{T}" /> representing a failed operation.</returns>
	public static new Result<T> Fail(string errorMessage)
	{
		return new Result<T>(default, false, errorMessage);
	}

	/// <summary>
	///   Creates a failed result with the specified error message and error code.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="code">One of the enumeration values that specifies the error code.</param>
	/// <returns>A <see cref="Result{T}" /> representing a failed operation.</returns>
	public static new Result<T> Fail(string errorMessage, ResultErrorCode code)
	{
		return new Result<T>(default, false, errorMessage, code);
	}

	/// <summary>
	///   Creates a failed result with the specified error message, error code, and details.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="code">One of the enumeration values that specifies the error code.</param>
	/// <param name="details">Optional structured error details.</param>
	/// <returns>A <see cref="Result{T}" /> representing a failed operation.</returns>
	public static new Result<T> Fail(string errorMessage, ResultErrorCode code, object? details)
	{
		return new Result<T>(default, false, errorMessage, code, details);
	}

	/// <summary>
	///   Implicitly converts a <see cref="Result{T}" /> to its value.
	/// </summary>
	/// <param name="result">The result to convert.</param>
	public static implicit operator T?(Result<T>? result)
	{
		if (result is null)
		{
			// Return the language default for T? when the Result is null. For value types this will
			// be the underlying default (e.g., 0 for int) which matches existing behavior.
			return default;
		}

		return result.Value;
	}

	/// <summary>
	///   Implicitly converts a value to a successful <see cref="Result{T}" />.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	public static implicit operator Result<T>(T? value)
	{
		return Ok(value);
	}
}
