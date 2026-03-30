// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ValidationBehavior.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Behaviors;

/// <summary>
///   MediatR pipeline behavior that runs FluentValidation validators before the handler.
///   Returns a <see cref="Result{T}" /> failure when validation errors are found.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
	IEnumerable<IValidator<TRequest>> validators)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		if (!validators.Any())
		{
			return await next();
		}

		var context = new ValidationContext<TRequest>(request);
		var failures = validators
			.Select(v => v.Validate(context))
			.SelectMany(r => r.Errors)
			.Where(f => f is not null)
			.ToList();

		if (failures.Count == 0)
		{
			return await next();
		}

		var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

		var responseType = typeof(TResponse);
		if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
		{
			var innerType = responseType.GetGenericArguments()[0];
			var failMethod = typeof(Result).GetMethods()
				.First(m => m.Name == "Fail"
					&& m.IsGenericMethodDefinition
					&& m.GetParameters().Length == 2);
			return (TResponse)failMethod.MakeGenericMethod(innerType)
				.Invoke(null, [errorMessage, ResultErrorCode.Validation])!;
		}

		throw new ValidationException(failures);
	}
}
