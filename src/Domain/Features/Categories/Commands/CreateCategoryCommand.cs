// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateCategoryCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Categories.Commands;

/// <summary>
///   Command to create a new category.
/// </summary>
public record CreateCategoryCommand(
	string CategoryName,
	string CategoryDescription) : IRequest<Result<CategoryDto>>;

/// <summary>
///   Handler for creating a new category.
/// </summary>
public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<CreateCategoryCommandHandler> _logger;

	public CreateCategoryCommandHandler(
		IRepository<Category> repository,
		ILogger<CreateCategoryCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Creating new category with name: {CategoryName}", request.CategoryName);

		// Check for duplicate category name
		var existingResult = await _repository.FirstOrDefaultAsync(
			c => c.CategoryName.ToLower() == request.CategoryName.ToLower() && !c.Archived,
			cancellationToken);

		if (existingResult.Success && existingResult.Value is not null)
		{
			_logger.LogWarning("Category with name '{CategoryName}' already exists", request.CategoryName);
			return Result.Fail<CategoryDto>("A category with this name already exists", ResultErrorCode.Conflict);
		}

		var category = new Category
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = request.CategoryName,
			CategoryDescription = request.CategoryDescription,
			DateCreated = DateTime.UtcNow,
			Archived = false,
			ArchivedBy = UserDto.Empty
		};

		var result = await _repository.AddAsync(category, cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to create category: {Error}", result.Error);
			return Result.Fail<CategoryDto>(result.Error ?? "Failed to create category", result.ErrorCode);
		}

		_logger.LogInformation("Successfully created category with ID: {CategoryId}", category.Id);
		return Result.Ok(new CategoryDto(result.Value!));
	}
}
