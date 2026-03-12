// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteAttachmentCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Features.Attachments.Commands;

namespace Domain.Features.Attachments.Validators;

/// <summary>
///   Validator for DeleteAttachmentCommand.
/// </summary>
public class DeleteAttachmentCommandValidator : AbstractValidator<DeleteAttachmentCommand>
{
	public DeleteAttachmentCommandValidator()
	{
		RuleFor(x => x.AttachmentId)
			.NotEmpty().WithMessage("Attachment ID is required")
			.Must(BeValidObjectId).WithMessage("Attachment ID must be a valid ObjectId");

		RuleFor(x => x.UserId)
			.NotEmpty().WithMessage("User ID is required");

		RuleFor(x => x.IsAdmin)
			.NotNull().WithMessage("IsAdmin flag is required");
	}

	private static bool BeValidObjectId(string id)
	{
		return ObjectId.TryParse(id, out _);
	}
}
