// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesByCategoryQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;
using Domain.Features.Analytics.Queries;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace Domain.Tests.Features.Analytics;

/// <summary>
/// Unit tests for GetIssuesByCategoryQueryHandler.
/// </summary>
public sealed class GetIssuesByCategoryQueryHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetIssuesByCategoryQueryHandler> _logger;
	private readonly GetIssuesByCategoryQueryHandler _sut;

	public GetIssuesByCategoryQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<GetIssuesByCategoryQueryHandler>>();
		_sut = new GetIssuesByCategoryQueryHandler(_repository, _logger);
	}

	[Fact]
	public async Task GetByCategory_ReturnsCountPerCategory()
	{
		// Arrange
		var query = new GetIssuesByCategoryQuery(null, null);

		var bugCategory = new CategoryDto(
			ObjectId.GenerateNewId(),
			"Bug",
			"Bug category",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var featureCategory = new CategoryDto(
			ObjectId.GenerateNewId(),
			"Feature",
			"Feature category",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var issues = new List<Issue>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 1",
				Status = StatusDto.Empty,
				Category = bugCategory,
				Author = UserDto.Empty,
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 2",
				Status = StatusDto.Empty,
				Category = bugCategory,
				Author = UserDto.Empty,
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 3",
				Status = StatusDto.Empty,
				Category = bugCategory,
				Author = UserDto.Empty,
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 4",
				Status = StatusDto.Empty,
				Category = featureCategory,
				Author = UserDto.Empty,
				DateCreated = DateTime.UtcNow
			}
		};

		_repository.FindAsync(Arg.Any<Expression<Func<Issue, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(2);
		result.Value!.First(c => c.Category == "Bug").Count.Should().Be(3);
		result.Value!.First(c => c.Category == "Feature").Count.Should().Be(1);
	}
}
