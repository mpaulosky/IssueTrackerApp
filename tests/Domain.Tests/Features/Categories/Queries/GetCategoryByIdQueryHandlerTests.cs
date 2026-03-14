// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetCategoryByIdQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Categories.Queries;

namespace Domain.Tests.Features.Categories.Queries;

/// <summary>
///   Unit tests for GetCategoryByIdQueryHandler.
/// </summary>
public class GetCategoryByIdQueryHandlerTests
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<GetCategoryByIdQueryHandler> _logger;
	private readonly GetCategoryByIdQueryHandler _handler;

	public GetCategoryByIdQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Category>>();
		_logger = new NullLogger<GetCategoryByIdQueryHandler>();
		_handler = new GetCategoryByIdQueryHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that GetById returns the category when it exists.
	/// </summary>
	[Fact]
	public async Task GetById_WhenExists_ReturnsCategory()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var category = new Category
		{
			Id = categoryId,
			CategoryName = "Test Category",
			CategoryDescription = "Test Description",
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var query = new GetCategoryByIdQuery(categoryId.ToString());

		_repository.GetByIdAsync(categoryId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(category));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(categoryId);
		result.Value.CategoryName.Should().Be("Test Category");
		result.Value.CategoryDescription.Should().Be("Test Description");
	}
}
