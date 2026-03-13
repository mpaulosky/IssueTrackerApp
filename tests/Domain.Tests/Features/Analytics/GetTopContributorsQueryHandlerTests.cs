// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetTopContributorsQueryHandlerTests.cs
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
/// Unit tests for GetTopContributorsQueryHandler.
/// </summary>
public sealed class GetTopContributorsQueryHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly IRepository<Comment> _commentRepository;
	private readonly ILogger<GetTopContributorsQueryHandler> _logger;
	private readonly GetTopContributorsQueryHandler _sut;

	public GetTopContributorsQueryHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_commentRepository = Substitute.For<IRepository<Comment>>();
		_logger = Substitute.For<ILogger<GetTopContributorsQueryHandler>>();
		_sut = new GetTopContributorsQueryHandler(_issueRepository, _commentRepository, _logger);
	}

	[Fact]
	public async Task GetTopContributors_ReturnsUsersByIssueCount()
	{
		// Arrange
		var query = new GetTopContributorsQuery(null, null, 10);

		var user1 = new UserDto("user1", "John Doe", "john@example.com");
		var user2 = new UserDto("user2", "Jane Smith", "jane@example.com");

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
				Status = closedStatus,
				Category = CategoryDto.Empty,
				Author = user1,
				DateCreated = DateTime.UtcNow.AddDays(-5),
				DateModified = DateTime.UtcNow.AddDays(-3),
				Archived = false
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 2",
				Status = closedStatus,
				Category = CategoryDto.Empty,
				Author = user1,
				DateCreated = DateTime.UtcNow.AddDays(-4),
				DateModified = DateTime.UtcNow.AddDays(-2),
				Archived = false
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Issue 3",
				Status = closedStatus,
				Category = CategoryDto.Empty,
				Author = user2,
				DateCreated = DateTime.UtcNow.AddDays(-3),
				DateModified = DateTime.UtcNow.AddDays(-1),
				Archived = false
			}
		};

		var comments = new List<Comment>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Comment 1",
				Author = user1,
				DateCreated = DateTime.UtcNow.AddDays(-2)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Comment 2",
				Author = user2,
				DateCreated = DateTime.UtcNow.AddDays(-1)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Comment 3",
				Author = user2,
				DateCreated = DateTime.UtcNow.AddDays(-1)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Comment 4",
				Author = user2,
				DateCreated = DateTime.UtcNow
			}
		};

		_issueRepository.FindAsync(Arg.Any<Expression<Func<Issue, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		_commentRepository.FindAsync(Arg.Any<Expression<Func<Comment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Comment>>(comments));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCountGreaterThan(0);

		var topContributor = result.Value!.First();
		// User2 has 1 issue closed + 3 comments = 4 total
		// User1 has 2 issues closed + 1 comment = 3 total
		// User2 should be first
		topContributor.UserId.Should().Be("user2");
		topContributor.IssuesClosed.Should().Be(1);
		topContributor.CommentsCount.Should().Be(3);
	}
}
