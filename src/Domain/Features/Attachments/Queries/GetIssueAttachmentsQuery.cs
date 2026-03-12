// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssueAttachmentsQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Attachments.Queries;

/// <summary>
///   Query to get all attachments for an issue.
/// </summary>
public record GetIssueAttachmentsQuery(string IssueId) : IRequest<Result<IEnumerable<AttachmentDto>>>;

/// <summary>
///   Handler for getting all attachments for an issue.
/// </summary>
public sealed class GetIssueAttachmentsQueryHandler
	: IRequestHandler<GetIssueAttachmentsQuery, Result<IEnumerable<AttachmentDto>>>
{
	private readonly IRepository<Attachment> _repository;
	private readonly ILogger<GetIssueAttachmentsQueryHandler> _logger;

	public GetIssueAttachmentsQueryHandler(
		IRepository<Attachment> repository,
		ILogger<GetIssueAttachmentsQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IEnumerable<AttachmentDto>>> Handle(
		GetIssueAttachmentsQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Getting attachments for issue {IssueId}", request.IssueId);

		var issueId = ObjectId.Parse(request.IssueId);
		var result = await _repository.FindAsync(
			a => a.IssueId == issueId,
			cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to get attachments: {Error}", result.Error);
			return Result.Fail<IEnumerable<AttachmentDto>>(
				result.Error ?? "Failed to get attachments",
				result.ErrorCode);
		}

		var attachments = result.Value!
			.Select(a => new AttachmentDto(a))
			.OrderByDescending(a => a.UploadedAt)
			.ToList();

		_logger.LogInformation(
			"Found {Count} attachments for issue {IssueId}",
			attachments.Count,
			request.IssueId);

		return Result.Ok<IEnumerable<AttachmentDto>>(attachments);
	}
}
