// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateIssueCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Mappers;

namespace Domain.Features.Issues.Commands;

/// <summary>
///   Command to update an existing issue.
/// </summary>
public record UpdateIssueCommand(
	string Id,
	string Title,
	string Description,
	CategoryDto Category) : IRequest<Result<IssueDto>>;

/// <summary>
///   Handler for updating an existing issue.
/// </summary>
public sealed class UpdateIssueCommandHandler : IRequestHandler<UpdateIssueCommand, Result<IssueDto>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<UpdateIssueCommandHandler> _logger;

	public UpdateIssueCommandHandler(
		IRepository<Issue> repository,
		ILogger<UpdateIssueCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IssueDto>> Handle(UpdateIssueCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating issue with ID: {IssueId}", request.Id);

		var existingResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.Id);
			return Result.Fail<IssueDto>("Issue not found", ResultErrorCode.NotFound);
		}

		var issue = existingResult.Value;
		issue.Title = request.Title;
		issue.Description = request.Description;
		issue.Category = CategoryMapper.ToInfo(request.Category);
		issue.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(issue, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to update issue: {Error}", result.Error);
			return Result.Fail<IssueDto>(result.Error ?? "Failed to update issue", result.ErrorCode);
		}

		_logger.LogInformation("Successfully updated issue with ID: {IssueId}", request.Id);
		return Result.Ok(new IssueDto(result.Value!));
	}
}
