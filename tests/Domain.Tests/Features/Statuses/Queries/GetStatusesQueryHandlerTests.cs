// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetStatusesQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Statuses.Queries;

namespace Domain.Tests.Features.Statuses.Queries;

/// <summary>
///   Unit tests for GetStatusesQueryHandler.
/// </summary>
public class GetStatusesQueryHandlerTests
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<GetStatusesQueryHandler> _logger;
	private readonly GetStatusesQueryHandler _handler;

	public GetStatusesQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Status>>();
		_logger = new NullLogger<GetStatusesQueryHandler>();
		_handler = new GetStatusesQueryHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that GetStatuses returns all active (non-archived) statuses.
	/// </summary>
	[Fact]
	public async Task GetStatuses_ReturnsAllActiveStatuses()
	{
		// Arrange
		var statuses = new List<Status>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Status A",
				StatusDescription = "Description A",
				Archived = false,
				ArchivedBy = UserInfo.Empty
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Status B",
				StatusDescription = "Description B",
				Archived = false,
				ArchivedBy = UserInfo.Empty
			}
		};

		var query = new GetStatusesQuery(IncludeArchived: false);

		_repository.FindAsync(
				Arg.Any<Expression<Func<Status, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Status>>(statuses));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Count().Should().Be(2);
		result.Value.Should().AllSatisfy(s => s.Archived.Should().BeFalse());
	}
}
