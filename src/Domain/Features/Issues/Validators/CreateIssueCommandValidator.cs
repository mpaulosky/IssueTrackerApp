// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateIssueCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Features.Issues.Commands;

namespace Domain.Features.Issues.Validators;

/// <summary>
///   Validator for CreateIssueCommand.
/// </summary>
public sealed class CreateIssueCommandValidator : AbstractValidator<CreateIssueCommand>
{
	public CreateIssueCommandValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(200)
			.WithMessage("Title must not exceed 200 characters")
			.MinimumLength(5)
			.WithMessage("Title must be at least 5 characters");

		RuleFor(x => x.Description)
			.NotEmpty()
			.WithMessage("Description is required")
			.MaximumLength(5000)
			.WithMessage("Description must not exceed 5000 characters")
			.MinimumLength(10)
			.WithMessage("Description must be at least 10 characters");

		RuleFor(x => x.Category)
			.NotNull()
			.WithMessage("Category is required");

		RuleFor(x => x.Category.CategoryName)
			.NotEmpty()
			.WithMessage("Category name is required")
			.When(x => x.Category is not null);

		RuleFor(x => x.Author)
			.NotNull()
			.WithMessage("Author is required");

		RuleFor(x => x.Author.Id)
			.NotEmpty()
			.WithMessage("Author ID is required")
			.When(x => x.Author is not null);

		RuleFor(x => x.Author.Name)
			.NotEmpty()
			.WithMessage("Author name is required")
			.When(x => x.Author is not null);
	}
}
