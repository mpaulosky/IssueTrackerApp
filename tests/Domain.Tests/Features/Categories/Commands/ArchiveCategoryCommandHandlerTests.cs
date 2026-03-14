// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ArchiveCategoryCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Categories.Commands;

namespace Domain.Tests.Features.Categories.Commands;

/// <summary>
///   Unit tests for ArchiveCategoryCommandHandler.
/// </summary>
public class ArchiveCategoryCommandHandlerTests
{
	private readonly IRepository<Category> _repository;
	private readonly ILogger<ArchiveCategoryCommandHandler> _logger;
	private readonly ArchiveCategoryCommandHandler _handler;

	public ArchiveCategoryCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Category>>();
		_logger = new NullLogger<ArchiveCategoryCommandHandler>();
		_handler = new ArchiveCategoryCommandHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that archiving a category sets the Archived flag to true.
	/// </summary>
	[Fact]
	public async Task ArchiveCategory_SetsArchivedFlag()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var existingCategory = new Category
		{
			Id = categoryId,
			CategoryName = "Test Category",
			CategoryDescription = "Test Description",
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var archivedByUser = new UserInfo { Id = "user-123", Name = "Test User", Email = "test@example.com" };
		var archivedByUserDto = new UserDto(archivedByUser);
		var command = new ArchiveCategoryCommand(categoryId.ToString(), true, archivedByUserDto);

		_repository.GetByIdAsync(categoryId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingCategory));

		Category? capturedCategory = null;
		_repository.UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedCategory = callInfo.Arg<Category>();
				return Result.Ok(capturedCategory);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedCategory.Should().NotBeNull();
		capturedCategory!.Archived.Should().BeTrue();
		capturedCategory.ArchivedBy.Should().Be(archivedByUser);
	}
}
