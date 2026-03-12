// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateStatusCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Features.Statuses.Commands;

namespace Domain.Features.Statuses.Validators;

/// <summary>
///   Validator for UpdateStatusCommand.
/// </summary>
public sealed class UpdateStatusCommandValidator : AbstractValidator<UpdateStatusCommand>
{
	public UpdateStatusCommandValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty()
			.WithMessage("Status ID is required");

		RuleFor(x => x.StatusName)
			.NotEmpty()
			.WithMessage("Status name is required")
			.MaximumLength(100)
			.WithMessage("Status name must not exceed 100 characters")
			.MinimumLength(2)
			.WithMessage("Status name must be at least 2 characters");

		RuleFor(x => x.StatusDescription)
			.NotEmpty()
			.WithMessage("Status description is required")
			.MaximumLength(500)
			.WithMessage("Status description must not exceed 500 characters")
			.MinimumLength(5)
			.WithMessage("Status description must be at least 5 characters");
	}
}
