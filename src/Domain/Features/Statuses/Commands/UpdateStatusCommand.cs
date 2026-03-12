// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateStatusCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Statuses.Commands;

/// <summary>
///   Command to update an existing status.
/// </summary>
public record UpdateStatusCommand(
	string Id,
	string StatusName,
	string StatusDescription) : IRequest<Result<StatusDto>>;

/// <summary>
///   Handler for updating an existing status.
/// </summary>
public sealed class UpdateStatusCommandHandler : IRequestHandler<UpdateStatusCommand, Result<StatusDto>>
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<UpdateStatusCommandHandler> _logger;

	public UpdateStatusCommandHandler(
		IRepository<Status> repository,
		ILogger<UpdateStatusCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<StatusDto>> Handle(UpdateStatusCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating status with ID: {StatusId}", request.Id);

		var existingResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Status not found with ID: {StatusId}", request.Id);
			return Result.Fail<StatusDto>("Status not found", ResultErrorCode.NotFound);
		}

		// Check for duplicate status name (excluding current)
		var duplicateResult = await _repository.FirstOrDefaultAsync(
			s => s.StatusName.ToLower() == request.StatusName.ToLower()
				&& s.Id.ToString() != request.Id
				&& !s.Archived,
			cancellationToken);

		if (duplicateResult.Success && duplicateResult.Value is not null)
		{
			_logger.LogWarning("Status with name '{StatusName}' already exists", request.StatusName);
			return Result.Fail<StatusDto>("A status with this name already exists", ResultErrorCode.Conflict);
		}

		var status = existingResult.Value;
		status.StatusName = request.StatusName;
		status.StatusDescription = request.StatusDescription;
		status.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(status, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to update status: {Error}", result.Error);
			return Result.Fail<StatusDto>(result.Error ?? "Failed to update status", result.ErrorCode);
		}

		_logger.LogInformation("Successfully updated status with ID: {StatusId}", request.Id);
		return Result.Ok(new StatusDto(result.Value!));
	}
}
