// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetStatusByIdQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Statuses.Queries;

/// <summary>
///   Query to get a single status by ID.
/// </summary>
public record GetStatusByIdQuery(string Id) : IRequest<Result<StatusDto>>;

/// <summary>
///   Handler for getting a single status by ID.
/// </summary>
public sealed class GetStatusByIdQueryHandler : IRequestHandler<GetStatusByIdQuery, Result<StatusDto>>
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<GetStatusByIdQueryHandler> _logger;

	public GetStatusByIdQueryHandler(
		IRepository<Status> repository,
		ILogger<GetStatusByIdQueryHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<StatusDto>> Handle(GetStatusByIdQuery request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching status with ID: {StatusId}", request.Id);

		var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (result.Failure || result.Value is null)
		{
			_logger.LogWarning("Status not found with ID: {StatusId}", request.Id);
			return Result.Fail<StatusDto>("Status not found", ResultErrorCode.NotFound);
		}

		_logger.LogInformation("Successfully fetched status with ID: {StatusId}", request.Id);
		return Result.Ok(new StatusDto(result.Value));
	}
}
