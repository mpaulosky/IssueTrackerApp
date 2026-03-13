// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetCategoriesQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Categories.Queries;

namespace Domain.Tests.Features.Categories.Queries;

/// <summary>
///   Unit tests for GetCategoriesQueryHandler.
/// </summary>
public class GetCategoriesQueryHandlerTests
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<GetCategoriesQueryHandler> _logger;
	private readonly GetCategoriesQueryHandler _handler;

	public GetCategoriesQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Category>>();
		_logger = new NullLogger<GetCategoriesQueryHandler>();
		_handler = new GetCategoriesQueryHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that GetCategories returns all active (non-archived) categories.
	/// </summary>
	[Fact]
	public async Task GetCategories_ReturnsAllActiveCategories()
	{
		// Arrange
		var categories = new List<Category>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Category A",
				CategoryDescription = "Description A",
				Archived = false,
				ArchivedBy = UserDto.Empty
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Category B",
				CategoryDescription = "Description B",
				Archived = false,
				ArchivedBy = UserDto.Empty
			}
		};

		var query = new GetCategoriesQuery(IncludeArchived: false);

		_repository.FindAsync(
				Arg.Any<Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(categories));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Count().Should().Be(2);
		result.Value.Should().AllSatisfy(c => c.Archived.Should().BeFalse());
	}

	/// <summary>
	///   Verifies that GetCategories excludes archived categories when IncludeArchived is false.
	/// </summary>
	[Fact]
	public async Task GetCategories_ExcludesArchived()
	{
		// Arrange
		var activeCategories = new List<Category>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				CategoryName = "Active Category",
				CategoryDescription = "Description",
				Archived = false,
				ArchivedBy = UserDto.Empty
			}
		};

		var query = new GetCategoriesQuery(IncludeArchived: false);

		_repository.FindAsync(
				Arg.Any<Expression<Func<Category, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Category>>(activeCategories));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(1);

		await _repository.Received(1).FindAsync(
			Arg.Any<Expression<Func<Category, bool>>>(),
			Arg.Any<CancellationToken>());

		await _repository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
	}
}
