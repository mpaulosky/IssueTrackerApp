// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssueByIdQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Issues.Queries;

/// <summary>
///   Query to get a single issue by ID.
/// </summary>
public record GetIssueByIdQuery(string Id) : IRequest<Result<IssueDto>>;

/// <summary>
///   Handler for getting a single issue by ID.
/// </summary>
public sealed class GetIssueByIdQueryHandler : IRequestHandler<GetIssueByIdQuery, Result<IssueDto>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetIssueByIdQueryHandler> _logger;

	public GetIssueByIdQueryHandler(
		IRepository<Issue> repository,
		ILogger<GetIssueByIdQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IssueDto>> Handle(GetIssueByIdQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching issue with ID: {IssueId}", request.Id);

		var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (result.Failure || result.Value is null)
		{
			_logger.LogWarning("Issue not found with ID: {IssueId}", request.Id);
			return Result.Fail<IssueDto>("Issue not found", ResultErrorCode.NotFound);
		}

		_logger.LogInformation("Successfully fetched issue with ID: {IssueId}", request.Id);
		return Result.Ok(new IssueDto(result.Value));
	}
}
