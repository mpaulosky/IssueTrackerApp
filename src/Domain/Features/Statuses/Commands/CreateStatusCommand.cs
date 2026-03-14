// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateStatusCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Statuses.Commands;

/// <summary>
///   Command to create a new status.
/// </summary>
public record CreateStatusCommand(
	string StatusName,
	string StatusDescription) : IRequest<Result<StatusDto>>;

/// <summary>
///   Handler for creating a new status.
/// </summary>
public sealed class CreateStatusCommandHandler : IRequestHandler<CreateStatusCommand, Result<StatusDto>>
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<CreateStatusCommandHandler> _logger;

	public CreateStatusCommandHandler(
		IRepository<Status> repository,
		ILogger<CreateStatusCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<StatusDto>> Handle(CreateStatusCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Creating new status with name: {StatusName}", request.StatusName);

		// Check for duplicate status name
		var existingResult = await _repository.FirstOrDefaultAsync(
			s => s.StatusName.ToLower() == request.StatusName.ToLower() && !s.Archived,
			cancellationToken);

		if (existingResult.Success && existingResult.Value is not null)
		{
			_logger.LogWarning("Status with name '{StatusName}' already exists", request.StatusName);
			return Result.Fail<StatusDto>("A status with this name already exists", ResultErrorCode.Conflict);
		}

		var status = new Status
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = request.StatusName,
			StatusDescription = request.StatusDescription,
			DateCreated = DateTime.UtcNow,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var result = await _repository.AddAsync(status, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to create status: {Error}", result.Error);
			return Result.Fail<StatusDto>(result.Error ?? "Failed to create status", result.ErrorCode);
		}

		_logger.LogInformation("Successfully created status with ID: {StatusId}", status.Id);
		return Result.Ok(new StatusDto(result.Value!));
	}
}
