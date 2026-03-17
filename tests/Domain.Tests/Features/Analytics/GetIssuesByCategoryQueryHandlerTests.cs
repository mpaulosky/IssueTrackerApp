// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesByCategoryQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using System.Linq.Expressions;

using Domain.Abstractions;
using Domain.Features.Analytics.Queries;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

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

		var bugCategory = new CategoryInfo
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Bug",
			CategoryDescription = "Bug category",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var featureCategory = new CategoryInfo
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Feature",
			CategoryDescription = "Feature category",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var issues = new List<Issue>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 1",
				Status = StatusInfo.Empty,
				Category = bugCategory,
				Author = UserInfo.Empty,
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 2",
				Status = StatusInfo.Empty,
				Category = bugCategory,
				Author = UserInfo.Empty,
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 3",
				Status = StatusInfo.Empty,
				Category = bugCategory,
				Author = UserInfo.Empty,
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 4",
				Status = StatusInfo.Empty,
				Category = featureCategory,
				Author = UserInfo.Empty,
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
