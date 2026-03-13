// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssuesByStatusQueryHandlerTests.cs
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
/// Unit tests for GetIssuesByStatusQueryHandler.
/// </summary>
public sealed class GetIssuesByStatusQueryHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<GetIssuesByStatusQueryHandler> _logger;
	private readonly GetIssuesByStatusQueryHandler _sut;

	public GetIssuesByStatusQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<GetIssuesByStatusQueryHandler>>();
		_sut = new GetIssuesByStatusQueryHandler(_repository, _logger);
	}

	[Fact]
	public async Task GetByStatus_ReturnsCountPerStatus()
	{
		// Arrange
		var query = new GetIssuesByStatusQuery(null, null);

		var openStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"Open",
			"Open status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var closedStatus = new StatusDto(
			ObjectId.GenerateNewId(),
			"Closed",
			"Closed status",
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
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 2",
				Status = openStatus,
				Category = CategoryDto.Empty,
				Author = UserDto.Empty,
				DateCreated = DateTime.UtcNow
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 3",
				Status = closedStatus,
				Category = CategoryDto.Empty,
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
		result.Value!.First(s => s.Status == "Open").Count.Should().Be(2);
		result.Value!.First(s => s.Status == "Closed").Count.Should().Be(1);
	}
}
