// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateCategoryCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Categories.Commands;

namespace Domain.Tests.Features.Categories.Commands;

/// <summary>
///   Unit tests for CreateCategoryCommandHandler.
/// </summary>
public class CreateCategoryCommandHandlerTests
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<CreateCategoryCommandHandler> _logger;
	private readonly CreateCategoryCommandHandler _handler;

	public CreateCategoryCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Category>>();
		_logger = new NullLogger<CreateCategoryCommandHandler>();
		_handler = new CreateCategoryCommandHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that creating a category with valid data returns success.
	/// </summary>
	[Fact]
	public async Task CreateCategory_WithValidData_ReturnsSuccess()
	{
		// Arrange
		var command = new CreateCategoryCommand("Test Category", "Test Description");

		_repository.FirstOrDefaultAsync(
				Arg.Any<Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<Category?>(null));

		_repository.AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>())
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
		result.Value!.CategoryName.Should().Be("Test Category");
		result.Value.CategoryDescription.Should().Be("Test Description");
	}

	/// <summary>
	///   Verifies that creating a category saves to the database via repository.
	/// </summary>
	[Fact]
	public async Task CreateCategory_SavesToDatabaseViaRepository()
	{
		// Arrange
		var command = new CreateCategoryCommand("New Category", "New Description");

		_repository.FirstOrDefaultAsync(
				Arg.Any<Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<Category?>(null));

		_repository.AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var category = callInfo.Arg<Category>();
				return Result.Ok(category);
			});

		// Act
		await _handler.Handle(command, CancellationToken.None);

		// Assert
		await _repository.Received(1).AddAsync(
			Arg.Is<Category>(c =>
				c.CategoryName == "New Category" &&
				c.CategoryDescription == "New Description" &&
				!c.Archived),
			Arg.Any<CancellationToken>());
	}
}
