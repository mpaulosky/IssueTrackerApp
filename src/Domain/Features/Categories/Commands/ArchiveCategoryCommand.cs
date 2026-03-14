// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ArchiveCategoryCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Mappers;

namespace Domain.Features.Categories.Commands;

/// <summary>
///   Command to archive (soft delete) or unarchive a category.
/// </summary>
public record ArchiveCategoryCommand(
	string Id,
	bool Archive,
	UserDto ArchivedBy) : IRequest<Result<CategoryDto>>;

/// <summary>
///   Handler for archiving or unarchiving a category.
/// </summary>
public sealed class ArchiveCategoryCommandHandler : IRequestHandler<ArchiveCategoryCommand, Result<CategoryDto>>
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<ArchiveCategoryCommandHandler> _logger;

	public ArchiveCategoryCommandHandler(
		IRepository<Category> repository,
		ILogger<ArchiveCategoryCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<CategoryDto>> Handle(ArchiveCategoryCommand request, CancellationToken cancellationToken)
	{
		var action = request.Archive ? "Archiving" : "Unarchiving";
		_logger.LogInformation("{Action} category with ID: {CategoryId}", action, request.Id);

		var existingResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Category not found with ID: {CategoryId}", request.Id);
			return Result.Fail<CategoryDto>("Category not found", ResultErrorCode.NotFound);
		}

		var category = existingResult.Value;
		category.Archived = request.Archive;
		category.ArchivedBy = request.Archive ? UserMapper.ToInfo(request.ArchivedBy) : UserInfo.Empty;
		category.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(category, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to {Action} category: {Error}", action.ToLower(), result.Error);
			return Result.Fail<CategoryDto>(result.Error ?? $"Failed to {action.ToLower()} category", result.ErrorCode);
		}

		_logger.LogInformation("Successfully {Action} category with ID: {CategoryId}", action.ToLower(), request.Id);
		return Result.Ok(new CategoryDto(result.Value!));
	}
}
