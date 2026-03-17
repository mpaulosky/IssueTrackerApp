// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetResolutionTimesQueryHandlerTests.cs
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
/// Unit tests for GetResolutionTimesQueryHandler.
/// </summary>
public sealed class GetResolutionTimesQueryHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetResolutionTimesQueryHandler> _logger;
	private readonly GetResolutionTimesQueryHandler _sut;

	public GetResolutionTimesQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<GetResolutionTimesQueryHandler>>();
		_sut = new GetResolutionTimesQueryHandler(_repository, _logger);
	}

	[Fact]
	public async Task GetResolutionTimes_CalculatesAverageTime()
	{
		// Arrange
		var query = new GetResolutionTimesQuery(null, null);

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

		var closedStatus = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Closed",
			StatusDescription = "Closed status",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var baseDate = DateTime.UtcNow.AddDays(-10);

		// Create issues with known resolution times
		// Issue 1: Created 48 hours before modified
		// Issue 2: Created 24 hours before modified
		// Average should be (48 + 24) / 2 = 36 hours
		var issues = new List<Issue>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Bug 1",
				Status = closedStatus,
				Category = bugCategory,
				Author = UserInfo.Empty,
				DateCreated = baseDate,
				DateModified = baseDate.AddHours(48)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Bug 2",
				Status = closedStatus,
				Category = bugCategory,
				Author = UserInfo.Empty,
				DateCreated = baseDate.AddDays(1),
				DateModified = baseDate.AddDays(1).AddHours(24)
			}
		};

		_repository.FindAsync(Arg.Any<Expression<Func<Issue, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(1);

		var bugResolution = result.Value!.First(r => r.Category == "Bug");
		bugResolution.AverageHours.Should().Be(36); // (48 + 24) / 2
	}
}
