// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ArchiveStatusCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Statuses.Commands;

/// <summary>
///   Command to archive (soft delete) or unarchive a status.
/// </summary>
public record ArchiveStatusCommand(
	string Id,
	bool Archive,
	UserDto ArchivedBy) : IRequest<Result<StatusDto>>;

/// <summary>
///   Handler for archiving or unarchiving a status.
/// </summary>
public sealed class ArchiveStatusCommandHandler : IRequestHandler<ArchiveStatusCommand, Result<StatusDto>>
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<ArchiveStatusCommandHandler> _logger;

	public ArchiveStatusCommandHandler(
		IRepository<Status> repository,
		ILogger<ArchiveStatusCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<StatusDto>> Handle(ArchiveStatusCommand request, CancellationToken cancellationToken)
	{
		var action = request.Archive ? "Archiving" : "Unarchiving";
		_logger.LogInformation("{Action} status with ID: {StatusId}", action, request.Id);

		var existingResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Status not found with ID: {StatusId}", request.Id);
			return Result.Fail<StatusDto>("Status not found", ResultErrorCode.NotFound);
		}

		var status = existingResult.Value;
		status.Archived = request.Archive;
		status.ArchivedBy = request.Archive ? request.ArchivedBy : UserDto.Empty;
		status.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(status, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to {Action} status: {Error}", action.ToLower(), result.Error);
			return Result.Fail<StatusDto>(result.Error ?? $"Failed to {action.ToLower()} status", result.ErrorCode);
		}

		_logger.LogInformation("Successfully {Action} status with ID: {StatusId}", action.ToLower(), request.Id);
		return Result.Ok(new StatusDto(result.Value!));
	}
}
