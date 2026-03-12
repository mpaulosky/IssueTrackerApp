// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateCategoryCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Categories.Commands;

/// <summary>
///   Command to update an existing category.
/// </summary>
public record UpdateCategoryCommand(
	string Id,
	string CategoryName,
	string CategoryDescription) : IRequest<Result<CategoryDto>>;

/// <summary>
///   Handler for updating an existing category.
/// </summary>
public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<UpdateCategoryCommandHandler> _logger;

	public UpdateCategoryCommandHandler(
		IRepository<Category> repository,
		ILogger<UpdateCategoryCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating category with ID: {CategoryId}", request.Id);

		var existingResult = await _repository.GetByIdAsync(request.Id, cancellationToken);

		if (existingResult.Failure || existingResult.Value is null)
		{
			_logger.LogWarning("Category not found with ID: {CategoryId}", request.Id);
			return Result.Fail<CategoryDto>("Category not found", ResultErrorCode.NotFound);
		}

		// Check for duplicate category name (excluding current)
		var duplicateResult = await _repository.FirstOrDefaultAsync(
			c => c.CategoryName.ToLower() == request.CategoryName.ToLower()
				&& c.Id.ToString() != request.Id
				&& !c.Archived,
			cancellationToken);

		if (duplicateResult.Success && duplicateResult.Value is not null)
		{
			_logger.LogWarning("Category with name '{CategoryName}' already exists", request.CategoryName);
			return Result.Fail<CategoryDto>("A category with this name already exists", ResultErrorCode.Conflict);
		}

		var category = existingResult.Value;
		category.CategoryName = request.CategoryName;
		category.CategoryDescription = request.CategoryDescription;
		category.DateModified = DateTime.UtcNow;

		var result = await _repository.UpdateAsync(category, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to update category: {Error}", result.Error);
			return Result.Fail<CategoryDto>(result.Error ?? "Failed to update category", result.ErrorCode);
		}

		_logger.LogInformation("Successfully updated category with ID: {CategoryId}", request.Id);
		return Result.Ok(new CategoryDto(result.Value!));
	}
}
