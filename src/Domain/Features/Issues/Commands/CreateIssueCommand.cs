// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateIssueCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to create a new issue.
/// </summary>
public record CreateIssueCommand(
	string Title,
	string Description,
	CategoryDto Category,
	UserDto Author) : IRequest<Result<IssueDto>>;

/// <summary>
///   Handler for creating a new issue.
/// </summary>
public sealed class CreateIssueCommandHandler : IRequestHandler<CreateIssueCommand, Result<IssueDto>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<CreateIssueCommandHandler> _logger;

	public CreateIssueCommandHandler(
		IRepository<Issue> repository,
		ILogger<CreateIssueCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IssueDto>> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Creating new issue with title: {Title}", request.Title);

		var issue = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = request.Title,
			Description = request.Description,
			Category = request.Category,
			Author = request.Author,
			Status = new StatusDto(
				ObjectId.Empty,
				"Open",
				"Issue is open and awaiting review",
				DateTime.UtcNow,
				null,
				false,
				UserDto.Empty),
			DateCreated = DateTime.UtcNow,
			ApprovedForRelease = false,
			Archived = false,
			Rejected = false
		};

		var result = await _repository.AddAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to create issue: {Error}", result.Error);
			return Result.Fail<IssueDto>(result.Error ?? "Failed to create issue", result.ErrorCode);
		}

		_logger.LogInformation("Successfully created issue with ID: {IssueId}", issue.Id);
		return Result.Ok(new IssueDto(result.Value!));
	}
}
