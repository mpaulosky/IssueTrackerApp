// Copyright (c) 2026. All rights reserved.

using Domain.Features.Issues.Commands;

namespace Domain.Features.Issues.Validators;

/// <summary>
///   Validator for VoteIssueCommand.
/// </summary>
public sealed class VoteIssueCommandValidator : AbstractValidator<VoteIssueCommand>
{
	public VoteIssueCommandValidator()
	{
		RuleFor(x => x.IssueId)
			.NotEmpty()
			.WithMessage("Issue ID is required");

		RuleFor(x => x.UserId)
			.NotEmpty()
			.WithMessage("User ID is required")
			.Must(id => !string.IsNullOrWhiteSpace(id))
			.WithMessage("User ID must not be whitespace");
	}
}

/// <summary>
///   Validator for UnvoteIssueCommand.
/// </summary>
public sealed class UnvoteIssueCommandValidator : AbstractValidator<UnvoteIssueCommand>
{
	public UnvoteIssueCommandValidator()
	{
		RuleFor(x => x.IssueId)
			.NotEmpty()
			.WithMessage("Issue ID is required");

		RuleFor(x => x.UserId)
			.NotEmpty()
			.WithMessage("User ID is required")
			.Must(id => !string.IsNullOrWhiteSpace(id))
			.WithMessage("User ID must not be whitespace");
	}
}
