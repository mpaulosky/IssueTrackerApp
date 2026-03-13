// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetAnalyticsSummaryQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Abstractions;
using Domain.DTOs.Analytics;
using Domain.Features.Analytics.Queries;
using Microsoft.Extensions.Logging;

namespace Domain.Tests.Features.Analytics;

/// <summary>
/// Unit tests for GetAnalyticsSummaryQueryHandler.
/// </summary>
public sealed class GetAnalyticsSummaryQueryHandlerTests
{
	private readonly IMediator _mediator;
	private readonly ILogger<GetAnalyticsSummaryQueryHandler> _logger;
	private readonly GetAnalyticsSummaryQueryHandler _sut;

	public GetAnalyticsSummaryQueryHandlerTests()
	{
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<GetAnalyticsSummaryQueryHandler>>();
		_sut = new GetAnalyticsSummaryQueryHandler(_mediator, _logger);
	}

	[Fact]
	public async Task GetSummary_ReturnsTotalIssueCount()
	{
		// Arrange
		var startDate = DateTime.UtcNow.AddDays(-30);
		var endDate = DateTime.UtcNow;
		var query = new GetAnalyticsSummaryQuery(startDate, endDate);

		var byStatus = new List<IssuesByStatusDto>
		{
			new("Open", 10),
			new("In Progress", 5),
			new("Closed", 15)
		};

		var byCategory = new List<IssuesByCategoryDto>
		{
			new("Bug", 20),
			new("Feature", 10)
		};

		var overTime = new List<IssuesOverTimeDto>
		{
			new(DateTime.UtcNow.Date, 5, 3)
		};

		var resolutionTimes = new List<ResolutionTimeDto>
		{
			new("Bug", 24.5)
		};

		var topContributors = new List<TopContributorDto>
		{
			new("user1", "User One", 10, 25)
		};

		_mediator.Send(Arg.Any<GetIssuesByStatusQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<IssuesByStatusDto>>(byStatus));

		_mediator.Send(Arg.Any<GetIssuesByCategoryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<IssuesByCategoryDto>>(byCategory));

		_mediator.Send(Arg.Any<GetIssuesOverTimeQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<IssuesOverTimeDto>>(overTime));

		_mediator.Send(Arg.Any<GetResolutionTimesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<ResolutionTimeDto>>(resolutionTimes));

		_mediator.Send(Arg.Any<GetTopContributorsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<TopContributorDto>>(topContributors));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalIssues.Should().Be(30); // 10 + 5 + 15
	}

	[Fact]
	public async Task GetSummary_ReturnsOpenClosedCounts()
	{
		// Arrange
		var query = new GetAnalyticsSummaryQuery(null, null);

		var byStatus = new List<IssuesByStatusDto>
		{
			new("Open", 10),
			new("In Progress", 5),
			new("Closed", 15)
		};

		var byCategory = new List<IssuesByCategoryDto>();
		var overTime = new List<IssuesOverTimeDto>();
		var resolutionTimes = new List<ResolutionTimeDto>();
		var topContributors = new List<TopContributorDto>();

		_mediator.Send(Arg.Any<GetIssuesByStatusQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<IssuesByStatusDto>>(byStatus));

		_mediator.Send(Arg.Any<GetIssuesByCategoryQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<IssuesByCategoryDto>>(byCategory));

		_mediator.Send(Arg.Any<GetIssuesOverTimeQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<IssuesOverTimeDto>>(overTime));

		_mediator.Send(Arg.Any<GetResolutionTimesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<ResolutionTimeDto>>(resolutionTimes));

		_mediator.Send(Arg.Any<GetTopContributorsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<TopContributorDto>>(topContributors));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.OpenIssues.Should().Be(15); // Open + In Progress
		result.Value!.ClosedIssues.Should().Be(15);
	}
}
