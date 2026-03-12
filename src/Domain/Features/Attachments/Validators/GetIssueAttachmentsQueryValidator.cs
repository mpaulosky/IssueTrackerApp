// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssueAttachmentsQueryValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Features.Attachments.Queries;

namespace Domain.Features.Attachments.Validators;

/// <summary>
///   Validator for GetIssueAttachmentsQuery.
/// </summary>
public class GetIssueAttachmentsQueryValidator : AbstractValidator<GetIssueAttachmentsQuery>
{
	public GetIssueAttachmentsQueryValidator()
	{
		RuleFor(x => x.IssueId)
			.NotEmpty().WithMessage("Issue ID is required")
			.Must(BeValidObjectId).WithMessage("Issue ID must be a valid ObjectId");
	}

	private static bool BeValidObjectId(string id)
	{
		return ObjectId.TryParse(id, out _);
	}
}
