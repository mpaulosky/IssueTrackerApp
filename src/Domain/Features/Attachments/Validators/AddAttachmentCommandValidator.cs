// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AddAttachmentCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Features.Attachments.Commands;

namespace Domain.Features.Attachments.Validators;

/// <summary>
///   Validator for AddAttachmentCommand.
/// </summary>
public class AddAttachmentCommandValidator : AbstractValidator<AddAttachmentCommand>
{
	public AddAttachmentCommandValidator()
	{
		RuleFor(x => x.IssueId)
			.NotEmpty().WithMessage("Issue ID is required")
			.Must(BeValidObjectId).WithMessage("Issue ID must be a valid ObjectId");

		RuleFor(x => x.FileName)
			.NotEmpty().WithMessage("File name is required")
			.MaximumLength(255).WithMessage("File name must not exceed 255 characters");

		RuleFor(x => x.ContentType)
			.NotEmpty().WithMessage("Content type is required")
			.Must(BeAllowedContentType).WithMessage(
				$"Content type must be one of: {string.Join(", ", FileValidationConstants.ALLOWED_CONTENT_TYPES)}");

		RuleFor(x => x.FileSize)
			.GreaterThan(0).WithMessage("File size must be greater than 0")
			.LessThanOrEqualTo(FileValidationConstants.MAX_FILE_SIZE)
			.WithMessage($"File size must not exceed {FileValidationConstants.MAX_FILE_SIZE / (1024 * 1024)}MB");

		RuleFor(x => x.FileContent)
			.NotNull().WithMessage("File content is required");

		RuleFor(x => x.UploadedBy)
			.NotNull().WithMessage("Uploaded by user is required");

		RuleFor(x => x.UploadedBy.Id)
			.NotEmpty().WithMessage("Uploaded by user ID is required")
			.When(x => x.UploadedBy != null);
	}

	private static bool BeValidObjectId(string id)
	{
		return ObjectId.TryParse(id, out _);
	}

	private static bool BeAllowedContentType(string contentType)
	{
		return FileValidationConstants.IsAllowedContentType(contentType);
	}
}
