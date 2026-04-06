// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     DashboardServiceCacheTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests
// =============================================

using Domain.Features.Dashboard.Queries;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for DashboardService cache-aside behaviour.
///   Uses a real MemoryDistributedCache so cache semantics are accurate.
/// </summary>
public sealed class DashboardServiceCacheTests
{
private readonly IMediator _mediator;
private readonly DashboardService _sut;

public DashboardServiceCacheTests()
{
_mediator = Substitute.For<IMediator>();

var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
var cacheLogger = Substitute.For<ILogger<DistributedCacheHelper>>();
var cacheHelper = new DistributedCacheHelper(cache, cacheLogger);

_sut = new DashboardService(_mediator, cacheHelper);
}

#region GetUserDashboardAsync cache tests

[Fact]
public async Task GetUserDashboardAsync_CallsMediatR_WhenCacheMiss()
{
// Arrange
var userId = "user-cache-miss";
var dashboard = CreateTestDashboardDto();
_mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
.Returns(Result.Ok(dashboard));

// Act — first call is always a cache miss
var result = await _sut.GetUserDashboardAsync(userId);

// Assert — MediatR must be invoked and result returned
result.Success.Should().BeTrue();
result.Value.Should().NotBeNull();
result.Value!.TotalIssues.Should().Be(10);
await _mediator.Received(1).Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>());
}

[Fact]
public async Task GetUserDashboardAsync_ReturnsCachedResult_WhenCacheHit()
{
// Arrange
var userId = "user-cache-hit";
var dashboard = CreateTestDashboardDto();
_mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
.Returns(Result.Ok(dashboard));

// Act — first call populates cache; second should serve from cache
await _sut.GetUserDashboardAsync(userId);
var result = await _sut.GetUserDashboardAsync(userId);

// Assert — MediatR called exactly once despite two service calls
result.Success.Should().BeTrue();
result.Value!.TotalIssues.Should().Be(10);
await _mediator.Received(1).Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>());
}

[Fact]
public async Task GetUserDashboardAsync_ReturnsSeparateCacheEntries_ForDifferentUsers()
{
// Arrange — two users with different dashboard data
var dashboardA = new UserDashboardDto(TotalIssues: 5, OpenIssues: 2, ResolvedIssues: 3,
ThisWeekIssues: 1, RecentIssues: []);
var dashboardB = new UserDashboardDto(TotalIssues: 20, OpenIssues: 15, ResolvedIssues: 5,
ThisWeekIssues: 4, RecentIssues: []);

_mediator.Send(Arg.Is<GetUserDashboardQuery>(q => q.UserId == "user-A"), Arg.Any<CancellationToken>())
.Returns(Result.Ok(dashboardA));
_mediator.Send(Arg.Is<GetUserDashboardQuery>(q => q.UserId == "user-B"), Arg.Any<CancellationToken>())
.Returns(Result.Ok(dashboardB));

// Act — each user has its own cache key
var resultA = await _sut.GetUserDashboardAsync("user-A");
var resultB = await _sut.GetUserDashboardAsync("user-B");

// Call again — should hit cache for both users
var resultA2 = await _sut.GetUserDashboardAsync("user-A");
var resultB2 = await _sut.GetUserDashboardAsync("user-B");

// Assert — two separate keys → exactly two MediatR calls total
resultA.Value!.TotalIssues.Should().Be(5);
resultB.Value!.TotalIssues.Should().Be(20);
resultA2.Value!.TotalIssues.Should().Be(5);
resultB2.Value!.TotalIssues.Should().Be(20);
await _mediator.Received(2).Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>());
}

[Fact]
public async Task GetUserDashboardAsync_DoesNotCacheFailedResults()
{
// Arrange
var userId = "user-fail";
_mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
.Returns(Result.Fail<UserDashboardDto>("Database error"));

// Act — call twice; failure must NOT be cached
await _sut.GetUserDashboardAsync(userId);
var result = await _sut.GetUserDashboardAsync(userId);

// Assert — MediatR called both times because nothing was cached
result.Success.Should().BeFalse();
await _mediator.Received(2).Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>());
}

#endregion

#region Helpers

private static UserDashboardDto CreateTestDashboardDto()
{
var recentIssues = new List<IssueDto>
{
CreateTestIssueDto("Recent Issue 1"),
CreateTestIssueDto("Recent Issue 2")
};

return new UserDashboardDto(
TotalIssues: 10,
OpenIssues: 5,
ResolvedIssues: 3,
ThisWeekIssues: 2,
RecentIssues: recentIssues);
}

private static IssueDto CreateTestIssueDto(string title)
{
return new IssueDto(
ObjectId.GenerateNewId(),
title,
"Test Description",
DateTime.UtcNow,
null,
new UserDto("user1", "Test User", "test@example.com"),
new CategoryDto(ObjectId.GenerateNewId(), "Test", "Desc", DateTime.UtcNow, null, false, UserDto.Empty),
new StatusDto(ObjectId.GenerateNewId(), "Open", "Open", DateTime.UtcNow, null, false, UserDto.Empty),
false,
UserDto.Empty,
false,
false,
UserDto.Empty,
0,
[],
[]);
}

#endregion
}
