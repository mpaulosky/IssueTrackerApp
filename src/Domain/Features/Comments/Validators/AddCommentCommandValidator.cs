// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddCommentCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Features.Comments.Commands;

namespace Domain.Features.Comments.Validators;

/// <summary>
///   Validator for AddCommentCommand.
/// </summary>
public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
	public AddCommentCommandValidator()
	{
		RuleFor(x => x.IssueId)
			.NotEmpty()
			.WithMessage("Issue ID is required")
			.Must(BeValidObjectId)
			.WithMessage("Issue ID must be a valid ObjectId");

		RuleFor(x => x.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(200)
			.WithMessage("Title must not exceed 200 characters")
			.MinimumLength(3)
			.WithMessage("Title must be at least 3 characters");

		RuleFor(x => x.Description)
			.NotEmpty()
			.WithMessage("Description is required")
			.MaximumLength(5000)
			.WithMessage("Description must not exceed 5000 characters")
			.MinimumLength(3)
			.WithMessage("Description must be at least 3 characters");

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

	private static bool BeValidObjectId(string id)
	{
		return ObjectId.TryParse(id, out _);
	}
}
