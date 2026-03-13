// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateCategoryCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Categories.Commands;

namespace Domain.Tests.Features.Categories.Commands;

/// <summary>
///   Unit tests for UpdateCategoryCommandHandler.
/// </summary>
public class UpdateCategoryCommandHandlerTests
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<UpdateCategoryCommandHandler> _logger;
	private readonly UpdateCategoryCommandHandler _handler;

	public UpdateCategoryCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Category>>();
		_logger = new NullLogger<UpdateCategoryCommandHandler>();
		_handler = new UpdateCategoryCommandHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that updating an existing category returns success.
	/// </summary>
	[Fact]
	public async Task UpdateCategory_WhenExists_ReturnsSuccess()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var existingCategory = new Category
		{
			Id = categoryId,
			CategoryName = "Old Name",
			CategoryDescription = "Old Description",
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ArchivedBy = UserDto.Empty
		};

		var command = new UpdateCategoryCommand(categoryId.ToString(), "New Name", "New Description");

		_repository.GetByIdAsync(categoryId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingCategory));

		_repository.FirstOrDefaultAsync(
				Arg.Any<Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<Category?>(null));

		_repository.UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var category = callInfo.Arg<Category>();
				return Result.Ok(category);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.CategoryName.Should().Be("New Name");
		result.Value.CategoryDescription.Should().Be("New Description");
	}

	/// <summary>
	///   Verifies that updating a non-existent category returns NotFound error.
	/// </summary>
	[Fact]
	public async Task UpdateCategory_WhenNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		var command = new UpdateCategoryCommand(nonExistentId, "Name", "Description");

		_repository.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Category>("Category not found", ResultErrorCode.NotFound));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Contain("not found");
	}
}
