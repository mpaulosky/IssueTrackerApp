// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CommentServiceCacheTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests
// =============================================

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for CommentService cache-aside behaviour.
///   Uses a real MemoryDistributedCache so cache semantics are accurate.
/// </summary>
public sealed class CommentServiceCacheTests
{
private readonly IMediator _mediator;
private readonly INotificationService _notificationService;
private readonly CommentService _sut;

public CommentServiceCacheTests()
{
_mediator = Substitute.For<IMediator>();
_notificationService = Substitute.For<INotificationService>();

var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
var cacheLogger = Substitute.For<ILogger<DistributedCacheHelper>>();
var cacheHelper = new DistributedCacheHelper(cache, cacheLogger);

_sut = new CommentService(_mediator, _notificationService, cacheHelper);

// Default stub for GetIssueByIdQuery (used by AddCommentAsync notification path).
var testIssue = IssueDto.Empty with
{
Id = ObjectId.GenerateNewId(),
Title = "Test Issue",
Author = new UserDto("user1", "Test User", "test@example.com")
};
_mediator.Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>())
.Returns(Result.Ok(testIssue));
}

#region GetCommentsAsync cache tests

[Fact]
public async Task GetCommentsByIssueIdAsync_CallsMediatR_WhenCacheMiss()
{
// Arrange
var issueId = "issue-abc";
var comments = new List<CommentDto> { CreateTestCommentDto("First") };
_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
.Returns(Result.Ok<IReadOnlyList<CommentDto>>(comments));

// Act — first call is always a cache miss
var result = await _sut.GetCommentsAsync(issueId);

// Assert — MediatR must be called on a miss and the result cached
result.Success.Should().BeTrue();
result.Value.Should().HaveCount(1);
await _mediator.Received(1).Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>());
}

[Fact]
public async Task GetCommentsByIssueIdAsync_ReturnsCachedResult_WhenCacheHit()
{
// Arrange
var issueId = "issue-hit";
var comments = new List<CommentDto>
{
CreateTestCommentDto("Cached Comment")
};
_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
.Returns(Result.Ok<IReadOnlyList<CommentDto>>(comments));

// Act — first call populates cache; second should serve from cache
await _sut.GetCommentsAsync(issueId);
var result = await _sut.GetCommentsAsync(issueId);

// Assert — MediatR called exactly once despite two service calls
result.Success.Should().BeTrue();
result.Value.Should().HaveCount(1);
result.Value!.First().Title.Should().Be("Cached Comment");
await _mediator.Received(1).Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>());
}

[Fact]
public async Task GetCommentsByIssueIdAsync_ReturnsFreshData_ForDifferentIssueIds()
{
// Arrange — two issues with different comment lists
var comments1 = new List<CommentDto> { CreateTestCommentDto("Issue1 Comment") };
var comments2 = new List<CommentDto>
{
CreateTestCommentDto("Issue2 CommentA"),
CreateTestCommentDto("Issue2 CommentB")
};

_mediator.Send(Arg.Is<GetIssueCommentsQuery>(q => q.IssueId == "issue-1"), Arg.Any<CancellationToken>())
.Returns(Result.Ok<IReadOnlyList<CommentDto>>(comments1));
_mediator.Send(Arg.Is<GetIssueCommentsQuery>(q => q.IssueId == "issue-2"), Arg.Any<CancellationToken>())
.Returns(Result.Ok<IReadOnlyList<CommentDto>>(comments2));

// Act
var result1 = await _sut.GetCommentsAsync("issue-1");
var result2 = await _sut.GetCommentsAsync("issue-2");

// Assert — separate cache keys → separate MediatR calls → separate data
result1.Value.Should().HaveCount(1);
result1.Value!.First().Title.Should().Be("Issue1 Comment");
result2.Value.Should().HaveCount(2);
await _mediator.Received(2).Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>());
}

#endregion

#region Write invalidation tests

[Fact]
public async Task CreateCommentAsync_InvalidatesCacheForIssue()
{
// Arrange
var issueId = "issue-create";
var existing = new List<CommentDto> { CreateTestCommentDto("Before") };
var fresh = new List<CommentDto>
{
CreateTestCommentDto("Before"),
CreateTestCommentDto("After")
};

// Return existing on first call, fresh on subsequent calls
_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
.Returns(
Result.Ok<IReadOnlyList<CommentDto>>(existing),
Result.Ok<IReadOnlyList<CommentDto>>(fresh));

_mediator.Send(Arg.Any<AddCommentCommand>(), Arg.Any<CancellationToken>())
.Returns(Result.Ok(CreateTestCommentDto("After")));

// Populate cache
await _sut.GetCommentsAsync(issueId);

// Act — create should invalidate
await _sut.AddCommentAsync(issueId, "After", "desc", new UserDto("u", "U", "u@u.com"));

// Second read should bypass cache and call MediatR again
var result = await _sut.GetCommentsAsync(issueId);

// Assert — MediatR called twice for GetIssueCommentsQuery (before and after invalidation)
result.Value.Should().HaveCount(2);
await _mediator.Received(2).Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>());
}

[Fact]
public async Task UpdateCommentAsync_InvalidatesCacheForIssue()
{
// Arrange
var issueId = "issue-update";
var before = new List<CommentDto> { CreateTestCommentDto("Original") };
var after = new List<CommentDto> { CreateTestCommentDto("Updated") };

_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
.Returns(
Result.Ok<IReadOnlyList<CommentDto>>(before),
Result.Ok<IReadOnlyList<CommentDto>>(after));

_mediator.Send(Arg.Any<UpdateCommentCommand>(), Arg.Any<CancellationToken>())
.Returns(Result.Ok(CreateTestCommentDto("Updated")));

// Populate cache
await _sut.GetCommentsAsync(issueId);

// Act — update should invalidate
await _sut.UpdateCommentAsync("comment-1", issueId, "Updated", "new desc", "user1");

// Third read should hit MediatR again
var result = await _sut.GetCommentsAsync(issueId);

// Assert
result.Value!.First().Title.Should().Be("Updated");
await _mediator.Received(2).Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>());
}

[Fact]
public async Task DeleteCommentAsync_InvalidatesCacheForIssue()
{
// Arrange
var issueId = "issue-delete";
var before = new List<CommentDto>
{
CreateTestCommentDto("Keep"),
CreateTestCommentDto("Delete Me")
};
var after = new List<CommentDto> { CreateTestCommentDto("Keep") };

_mediator.Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>())
.Returns(
Result.Ok<IReadOnlyList<CommentDto>>(before),
Result.Ok<IReadOnlyList<CommentDto>>(after));

_mediator.Send(Arg.Any<DeleteCommentCommand>(), Arg.Any<CancellationToken>())
.Returns(Result.Ok(true));

// Populate cache
await _sut.GetCommentsAsync(issueId);

// Act — delete should invalidate
await _sut.DeleteCommentAsync("comment-del", issueId, "user1", false,
new UserDto("user1", "User", "u@u.com"));

// Read should call MediatR again
var result = await _sut.GetCommentsAsync(issueId);

// Assert
result.Value.Should().HaveCount(1);
await _mediator.Received(2).Send(Arg.Any<GetIssueCommentsQuery>(), Arg.Any<CancellationToken>());
}

#endregion

#region Helpers

private static CommentDto CreateTestCommentDto(string title)
{
return new CommentDto(
ObjectId.GenerateNewId(),
title,
"Test Description",
DateTime.UtcNow,
null,
ObjectId.GenerateNewId(),
new UserDto("user1", "Test User", "test@example.com"),
[],
false,
UserDto.Empty,
false,
UserDto.Empty);
}

#endregion
}
