// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateCategoryCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Features.Categories.Commands;

namespace Domain.Features.Categories.Validators;

/// <summary>
///   Validator for UpdateCategoryCommand.
/// </summary>
public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
	public UpdateCategoryCommandValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty()
			.WithMessage("Category ID is required");

		RuleFor(x => x.CategoryName)
			.NotEmpty()
			.WithMessage("Category name is required")
			.MaximumLength(100)
			.WithMessage("Category name must not exceed 100 characters")
			.MinimumLength(2)
			.WithMessage("Category name must be at least 2 characters");

		RuleFor(x => x.CategoryDescription)
			.NotEmpty()
			.WithMessage("Category description is required")
			.MaximumLength(500)
			.WithMessage("Category description must not exceed 500 characters")
			.MinimumLength(5)
			.WithMessage("Category description must be at least 5 characters");
	}
}
