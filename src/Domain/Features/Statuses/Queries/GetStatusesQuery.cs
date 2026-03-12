// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetStatusesQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Statuses.Queries;

/// <summary>
///   Query to get all statuses with optional filtering.
/// </summary>
public record GetStatusesQuery(bool IncludeArchived = false) : IRequest<Result<IEnumerable<StatusDto>>>;

/// <summary>
///   Handler for getting all statuses.
/// </summary>
public sealed class GetStatusesQueryHandler : IRequestHandler<GetStatusesQuery, Result<IEnumerable<StatusDto>>>
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<GetStatusesQueryHandler> _logger;

	public GetStatusesQueryHandler(
		IRepository<Status> repository,
		ILogger<GetStatusesQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<IEnumerable<StatusDto>>> Handle(
		GetStatusesQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching statuses - IncludeArchived: {IncludeArchived}", request.IncludeArchived);

		Result<IEnumerable<Status>> result;

		if (request.IncludeArchived)
		{
			result = await _repository.GetAllAsync(cancellationToken);
		}
		else
		{
			result = await _repository.FindAsync(s => !s.Archived, cancellationToken);
		}

		if (result.Failure)
		{
			_logger.LogError("Failed to fetch statuses: {Error}", result.Error);
			return Result.Fail<IEnumerable<StatusDto>>(
				result.Error ?? "Failed to fetch statuses",
				result.ErrorCode);
		}

		var statuses = result.Value?
			.OrderBy(s => s.StatusName)
			.Select(s => new StatusDto(s))
			?? Enumerable.Empty<StatusDto>();

		_logger.LogInformation("Successfully fetched {Count} statuses", statuses.Count());
		return Result.Ok(statuses);
	}
}
