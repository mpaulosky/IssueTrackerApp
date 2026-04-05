// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     IssueServiceCacheTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests
// =============================================

using Domain.Features.Issues.Commands;
using Domain.Features.Issues.Commands.Bulk;
using Domain.Features.Issues.Queries;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for IssueService cache-aside behaviour.
///   Uses a real MemoryDistributedCache so cache semantics are accurate.
/// </summary>
public sealed class IssueServiceCacheTests
{
	private readonly IMediator _mediator;
	private readonly Domain.Abstractions.INotificationService _notificationService;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IssueService _sut;

	public IssueServiceCacheTests()
	{
		_mediator = Substitute.For<IMediator>();
		_notificationService = Substitute.For<Domain.Abstractions.INotificationService>();
		_bulkQueue = Substitute.For<IBulkOperationQueue>();

		var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		var cacheLogger = Substitute.For<ILogger<DistributedCacheHelper>>();
		var cacheHelper = new DistributedCacheHelper(cache, cacheLogger);

		_sut = new IssueService(_mediator, _notificationService, _bulkQueue, cacheHelper);
	}

	#region GetIssueByIdAsync cache tests

	[Fact]
	public async Task GetIssueByIdAsync_CallsMediatR_WhenCacheMiss()
	{
		// Arrange
		var issueDto = CreateTestIssueDto("Test Issue");
		_mediator.Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		// Act
		var result = await _sut.GetIssueByIdAsync("issue-1");

		// Assert — first call is always a cache miss, so MediatR must be called
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("Test Issue");
		await _mediator.Received(1).Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssueByIdAsync_ReturnsCachedResult_WhenCacheHit()
	{
		// Arrange
		var issueDto = CreateTestIssueDto("Test Issue");
		_mediator.Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		// Act — first call populates cache; second should hit cache
		await _sut.GetIssueByIdAsync("issue-1");
		var result = await _sut.GetIssueByIdAsync("issue-1");

		// Assert — MediatR called exactly once despite two service calls
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("Test Issue");
		await _mediator.Received(1).Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssueByIdAsync_DifferentIds_UsesSeparateCacheEntries()
	{
		// Arrange
		var issue1 = CreateTestIssueDto("Issue One");
		var issue2 = CreateTestIssueDto("Issue Two");

		_mediator.Send(Arg.Is<GetIssueByIdQuery>(q => q.Id == "issue-1"), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue1));
		_mediator.Send(Arg.Is<GetIssueByIdQuery>(q => q.Id == "issue-2"), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue2));

		// Act
		var r1 = await _sut.GetIssueByIdAsync("issue-1");
		var r2 = await _sut.GetIssueByIdAsync("issue-2");

		// Assert — two separate keys → two mediator calls
		r1.Value!.Title.Should().Be("Issue One");
		r2.Value!.Title.Should().Be("Issue Two");
		await _mediator.Received(2).Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region GetIssuesAsync cache tests

	[Fact]
	public async Task GetIssuesAsync_HitsMediatR_OnCacheMiss()
	{
		// Arrange
		var issues = new List<IssueDto> { CreateTestIssueDto("Issue 1") };
		var response = new PaginatedResponse<IssueDto>(issues, 1, 1, 10);
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(response));

		// Act
		var result = await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(1);
		await _mediator.Received(1).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssuesAsync_ReturnsCachedPage_WhenVersionAndKeyMatch()
	{
		// Arrange
		var issues = new List<IssueDto> { CreateTestIssueDto("Issue 1") };
		var response = new PaginatedResponse<IssueDto>(issues, 1, 1, 10);
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(response));

		// Act — first call populates cache; second should serve from cache
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);
		var result = await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert — mediator called exactly once despite two service calls
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(1);
		await _mediator.Received(1).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssuesAsync_DifferentPages_UsesSeparateCacheKeys()
	{
		// Arrange
		var page1Response = new PaginatedResponse<IssueDto>(
			new List<IssueDto> { CreateTestIssueDto("Issue 1") }, 2, 1, 1);
		var page2Response = new PaginatedResponse<IssueDto>(
			new List<IssueDto> { CreateTestIssueDto("Issue 2") }, 2, 2, 1);

		_mediator.Send(Arg.Is<GetIssuesQuery>(q => q.Page == 1), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(page1Response));
		_mediator.Send(Arg.Is<GetIssuesQuery>(q => q.Page == 2), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(page2Response));

		// Act
		var r1 = await _sut.GetIssuesAsync(page: 1, pageSize: 1);
		var r2 = await _sut.GetIssuesAsync(page: 2, pageSize: 1);

		// Assert — separate page keys → two mediator calls
		r1.Value!.Page.Should().Be(1);
		r2.Value!.Page.Should().Be(2);
		await _mediator.Received(2).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Write invalidation tests

	[Fact]
	public async Task CreateIssueAsync_BumpsVersionCache()
	{
		// Arrange — prime the list cache
		var issues = new List<IssueDto> { CreateTestIssueDto("Existing") };
		var response = new PaginatedResponse<IssueDto>(issues, 1, 1, 10);
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(response));

		await _sut.GetIssuesAsync(page: 1, pageSize: 10);   // populates cache version=0

		var newIssue = CreateTestIssueDto("New Issue");
		_mediator.Send(Arg.Any<CreateIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(newIssue));

		// Act
		await _sut.CreateIssueAsync("New Issue", "Desc", CreateTestCategoryDto(), CreateTestUserDto());

		// Next read must re-hit MediatR because version was bumped (new key)
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert — GetIssuesQuery called twice: 1 before create + 1 after version bump
		await _mediator.Received(2).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdateIssueAsync_BumpsVersionAndRemovesByIdCache()
	{
		// Arrange — prime both caches
		var issueDto = CreateTestIssueDto("Original");
		_mediator.Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(new PaginatedResponse<IssueDto>(
				new List<IssueDto> { issueDto }, 1, 1, 10)));

		await _sut.GetIssueByIdAsync("issue-1");    // populates by-id cache
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);  // populates list cache

		var updated = CreateTestIssueDto("Updated");
		_mediator.Send(Arg.Any<UpdateIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updated));

		// Act
		await _sut.UpdateIssueAsync("issue-1", "Updated", "Desc", CreateTestCategoryDto());

		// Both reads should now miss cache and call MediatR again
		await _sut.GetIssueByIdAsync("issue-1");
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert — each query called once before update, once after
		await _mediator.Received(2).Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>());
		await _mediator.Received(2).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteIssueAsync_BumpsVersionAndRemovesByIdCache()
	{
		// Arrange — prime both caches
		var issueDto = CreateTestIssueDto("To Delete");
		_mediator.Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(new PaginatedResponse<IssueDto>(
				new List<IssueDto> { issueDto }, 1, 1, 10)));

		await _sut.GetIssueByIdAsync("issue-1");
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		_mediator.Send(Arg.Any<DeleteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		await _sut.DeleteIssueAsync("issue-1", CreateTestUserDto());

		// Both reads must re-hit MediatR
		await _sut.GetIssueByIdAsync("issue-1");
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert
		await _mediator.Received(2).Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>());
		await _mediator.Received(2).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ChangeIssueStatusAsync_BumpsVersionAndRemovesByIdCache()
	{
		// Arrange — prime both caches
		var issueDto = CreateTestIssueDto("Status Issue");
		_mediator.Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(new PaginatedResponse<IssueDto>(
				new List<IssueDto> { issueDto }, 1, 1, 10)));

		await _sut.GetIssueByIdAsync("issue-1");
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		var updatedIssue = CreateTestIssueDto("Status Issue");
		_mediator.Send(Arg.Any<ChangeIssueStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedIssue));

		// Act
		await _sut.ChangeIssueStatusAsync("issue-1", CreateTestStatusDto());

		// Both reads must re-hit MediatR
		await _sut.GetIssueByIdAsync("issue-1");
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert
		await _mediator.Received(2).Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>());
		await _mediator.Received(2).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task BulkUpdateStatusAsync_BumpsVersionCache()
	{
		// Arrange — prime list cache
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(new PaginatedResponse<IssueDto>(
				new List<IssueDto> { CreateTestIssueDto("Issue") }, 1, 1, 10)));

		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(BulkOperationResult.Success(1, "token")));

		// Act
		await _sut.BulkUpdateStatusAsync(["issue-1"], CreateTestStatusDto(), "user1");

		// Next read must re-hit MediatR
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert — called once before bulk + once after version bump
		await _mediator.Received(2).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UndoLastBulkOperationAsync_BumpsVersionCache()
	{
		// Arrange — prime list cache
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(new PaginatedResponse<IssueDto>(
				new List<IssueDto> { CreateTestIssueDto("Issue") }, 1, 1, 10)));

		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		_mediator.Send(Arg.Any<UndoBulkOperationCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(BulkOperationResult.Success(1)));

		// Act
		await _sut.UndoLastBulkOperationAsync("undo-token", "user1");

		// Next read must re-hit MediatR
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert — list re-fetched after undo invalidates version
		await _mediator.Received(2).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task WriteOperation_OnFailure_DoesNotBumpVersion()
	{
		// Arrange — prime list cache
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(new PaginatedResponse<IssueDto>(
				new List<IssueDto> { CreateTestIssueDto("Issue") }, 1, 1, 10)));

		await _sut.GetIssuesAsync(page: 1, pageSize: 10);   // version=0, cache populated

		_mediator.Send(Arg.Any<DeleteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Not found"));

		// Act — failed delete should NOT bump the version
		await _sut.DeleteIssueAsync("issue-1", CreateTestUserDto());

		// Read should still be served from cache (no version bump on failure)
		await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert — GetIssuesQuery called only once (second read served from cache)
		await _mediator.Received(1).Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Helper methods

	private static IssueDto CreateTestIssueDto(string title)
	{
		return new IssueDto(
			ObjectId.GenerateNewId(),
			title,
			"Test Description",
			DateTime.UtcNow,
			null,
			CreateTestUserDto(),
			CreateTestCategoryDto(),
			CreateTestStatusDto(),
			false,
			UserDto.Empty,
			false,
			false,
			UserDto.Empty,
			0,
			[],
			[]);
	}

	private static CategoryDto CreateTestCategoryDto()
	{
		return new CategoryDto(
			ObjectId.GenerateNewId(),
			"Test Category",
			"Category Description",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	private static StatusDto CreateTestStatusDto()
	{
		return new StatusDto(
			ObjectId.GenerateNewId(),
			"Open",
			"Open status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	private static UserDto CreateTestUserDto() =>
		new("user1", "Test User", "test@example.com");

	#endregion
}
