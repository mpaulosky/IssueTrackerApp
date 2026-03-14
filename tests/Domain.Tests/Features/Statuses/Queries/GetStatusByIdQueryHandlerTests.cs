// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetStatusByIdQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Statuses.Queries;

namespace Domain.Tests.Features.Statuses.Queries;

/// <summary>
///   Unit tests for GetStatusByIdQueryHandler.
/// </summary>
public class GetStatusByIdQueryHandlerTests
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<GetStatusByIdQueryHandler> _logger;
	private readonly GetStatusByIdQueryHandler _handler;

	public GetStatusByIdQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Status>>();
		_logger = new NullLogger<GetStatusByIdQueryHandler>();
		_handler = new GetStatusByIdQueryHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that GetById returns the status when it exists.
	/// </summary>
	[Fact]
	public async Task GetById_WhenExists_ReturnsStatus()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var status = new Status
		{
			Id = statusId,
			StatusName = "Test Status",
			StatusDescription = "Test Description",
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var query = new GetStatusByIdQuery(statusId.ToString());

		_repository.GetByIdAsync(statusId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(status));

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(statusId);
		result.Value.StatusName.Should().Be("Test Status");
		result.Value.StatusDescription.Should().Be("Test Description");
	}
}
