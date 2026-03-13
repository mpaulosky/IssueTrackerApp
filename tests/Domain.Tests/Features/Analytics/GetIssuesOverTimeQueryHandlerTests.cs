// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesOverTimeQueryHandlerTests.cs
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
/// Unit tests for GetIssuesOverTimeQueryHandler.
/// </summary>
public sealed class GetIssuesOverTimeQueryHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetIssuesOverTimeQueryHandler> _logger;
	private readonly GetIssuesOverTimeQueryHandler _sut;

	public GetIssuesOverTimeQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<GetIssuesOverTimeQueryHandler>>();
		_sut = new GetIssuesOverTimeQueryHandler(_repository, _logger);
	}

	[Fact]
	public async Task GetOverTime_ReturnsTimeSeriesData()
	{
		// Arrange
		var query = new GetIssuesOverTimeQuery(null, null);

		var today = DateTime.UtcNow.Date;
		var yesterday = today.AddDays(-1);

		var closedStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"Closed",
			"Closed status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var openStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"Open",
			"Open status",
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
				Status = openStatus,
				Category = CategoryDto.Empty,
				Author = UserDto.Empty,
				DateCreated = today.AddHours(10)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 2",
				Status = closedStatus,
				Category = CategoryDto.Empty,
				Author = UserDto.Empty,
				DateCreated = yesterday.AddHours(14),
				DateModified = today.AddHours(9)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 3",
				Status = openStatus,
				Category = CategoryDto.Empty,
				Author = UserDto.Empty,
				DateCreated = yesterday.AddHours(8)
			}
		};

		_repository.FindAsync(Arg.Any<Expression<Func<Issue, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCountGreaterThan(0);

		var todayData = result.Value!.FirstOrDefault(d => d.Date == today);
		todayData.Should().NotBeNull();
		todayData!.Created.Should().Be(1);
		todayData.Closed.Should().Be(1);
	}

	[Fact]
	public async Task GetOverTime_FiltersByDateRange()
	{
		// Arrange
		var startDate = DateTime.UtcNow.AddDays(-7);
		var endDate = DateTime.UtcNow;
		var query = new GetIssuesOverTimeQuery(startDate, endDate);

		var issues = new List<Issue>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Recent Issue",
				Status = StatusDto.Empty,
				Category = CategoryDto.Empty,
				Author = UserDto.Empty,
				DateCreated = DateTime.UtcNow.AddDays(-3)
			}
		};

		_repository.FindAsync(Arg.Any<Expression<Func<Issue, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();

		await _repository.Received(1).FindAsync(
			Arg.Any<Expression<Func<Issue, bool>>>(),
			Arg.Any<CancellationToken>());
	}
}
