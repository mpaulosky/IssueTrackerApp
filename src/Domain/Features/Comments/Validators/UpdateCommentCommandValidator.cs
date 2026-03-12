// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateCommentCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Features.Comments.Commands;

namespace Domain.Features.Comments.Validators;

/// <summary>
///   Validator for UpdateCommentCommand.
/// </summary>
public sealed class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
	public UpdateCommentCommandValidator()
	{
		RuleFor(x => x.CommentId)
			.NotEmpty()
			.WithMessage("Comment ID is required")
			.Must(BeValidObjectId)
			.WithMessage("Comment ID must be a valid ObjectId");

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

		RuleFor(x => x.RequestingUserId)
			.NotEmpty()
			.WithMessage("Requesting user ID is required");
	}

	private static bool BeValidObjectId(string id)
	{
		return ObjectId.TryParse(id, out _);
	}
}
