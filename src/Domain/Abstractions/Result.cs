namespace Domain.Abstractions;

/// <summary>
/// Represents the result of an operation with optional success value or error details.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T>
{
	private Result(bool isSuccess, T? value, Error? error)
	{
		IsSuccess = isSuccess;
		Value = value;
		Error = error;
	}

	public bool IsSuccess { get; }
	public bool IsFailure => !IsSuccess;
	public T? Value { get; }
	public Error? Error { get; }

	/// <summary>
	/// Creates a successful result with a value.
	/// </summary>
	public static Result<T> Success(T value) => new(true, value, null);

	/// <summary>
	/// Creates a failed result with an error.
	/// </summary>
	public static Result<T> Failure(Error error) => new(false, default, error);

	/// <summary>
	/// Maps the result value to a new type if successful.
	/// </summary>
	public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
	{
		return IsSuccess
			? Result<TNew>.Success(mapper(Value!))
			: Result<TNew>.Failure(Error!);
	}

	/// <summary>
	/// Executes an async operation on the result value if successful.
	/// </summary>
	public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
	{
		return IsSuccess
			? Result<TNew>.Success(await mapper(Value!))
			: Result<TNew>.Failure(Error!);
	}
}

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed record Error(string Code, string Message)
{
	public static readonly Error None = new(string.Empty, string.Empty);
	public static readonly Error NullValue = new("Error.NullValue", "The specified value is null.");
	
	public static Error NotFound(string entityName, object id) =>
		new("Error.NotFound", $"{entityName} with ID '{id}' was not found.");
	
	public static Error Validation(string message) =>
		new("Error.Validation", message);
	
	public static Error Conflict(string message) =>
		new("Error.Conflict", message);
	
	public static Error Failure(string message) =>
		new("Error.Failure", message);
}
