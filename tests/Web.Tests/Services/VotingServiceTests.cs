// =======================================================
// Copyright (c) 2026. All rights reserved.
// File Name :     VotingServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for VotingService facade operations.
///   Tests vote/unvote orchestration, authentication checks, and MediatR integration.
/// </summary>
public sealed class VotingServiceTests
{
	private readonly IMediator _mediator;
	private readonly ILogger<VotingService> _logger;

	public VotingServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<VotingService>>();
	}

	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	private VotingService CreateSut(string? userId, bool useSub = true)
	{
		var accessor = Substitute.For<IHttpContextAccessor>();

		if (userId is not null)
		{
			var context = new DefaultHttpContext();
			var claimType = useSub ? "sub" : ClaimTypes.NameIdentifier;
			var claims = new[] { new Claim(claimType, userId) };
			var identity = new ClaimsIdentity(claims, "Test");
			context.User = new ClaimsPrincipal(identity);
			accessor.HttpContext.Returns(context);
		}
		else
		{
			accessor.HttpContext.Returns((HttpContext?)null);
		}

		return new VotingService(_mediator, accessor, _logger);
	}

	/// <summary>
	///   Creates a VotingService where HttpContext is present but the user has NO
	///   "sub" or NameIdentifier claim — simulates a token without a subject claim.
	/// </summary>
	private VotingService CreateSutWithContextButNoClaims()
	{
		var accessor = Substitute.For<IHttpContextAccessor>();
		var context = new DefaultHttpContext();
		// Authenticated identity but irrelevant claim type
		var claims = new[] { new Claim(ClaimTypes.Email, "nobody@example.com") };
		var identity = new ClaimsIdentity(claims, "Test");
		context.User = new ClaimsPrincipal(identity);
		accessor.HttpContext.Returns(context);
		return new VotingService(_mediator, accessor, _logger);
	}

	private static IssueDto CreateTestIssueDto(int votes = 1, IReadOnlyList<string>? votedBy = null)
	{
		return new IssueDto(
			ObjectId.GenerateNewId(),
			"Test Issue",
			"Test Description",
			DateTime.UtcNow,
			DateTime.UtcNow,
			new UserDto("author-1", "Author One", "author@example.com"),
			new CategoryDto(ObjectId.GenerateNewId(), "Bug", "Bug Category", DateTime.UtcNow, null, false, UserDto.Empty),
			new StatusDto(ObjectId.GenerateNewId(), "Open", "Open", DateTime.UtcNow, null, false, UserDto.Empty),
			false,
			UserDto.Empty,
			false,
			false,
			UserDto.Empty,
			votes,
			votedBy ?? [],
			[]);
	}

	// -----------------------------------------------------------------------
	// VoteAsync — happy paths
	// -----------------------------------------------------------------------

	#region VoteAsync Happy Paths

	[Fact]
	public async Task Should_AddUpvote_When_UserHasNotVoted()
	{
		// Arrange
		const string issueId = "issue-abc";
		const string userId = "user-1";
		var issueDto = CreateTestIssueDto(votes: 1, votedBy: [userId]);

		_mediator.Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		var sut = CreateSut(userId);

		// Act
		var result = await sut.VoteAsync(issueId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Votes.Should().Be(1);
	}

	[Fact]
	public async Task Should_SendVoteIssueCommand_With_CorrectIssueIdAndUserId()
	{
		// Arrange
		const string issueId = "issue-xyz";
		const string userId = "user-99";
		var issueDto = CreateTestIssueDto();

		_mediator.Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		var sut = CreateSut(userId);

		// Act
		await sut.VoteAsync(issueId);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<VoteIssueCommand>(c =>
				c.IssueId == issueId &&
				c.UserId == userId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_AddUpvote_When_UserIdResolvedFromNameIdentifierClaim()
	{
		// Arrange
		const string issueId = "issue-ni";
		const string userId = "user-ni";
		var issueDto = CreateTestIssueDto(votes: 1, votedBy: [userId]);

		_mediator.Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		// useSub = false → claim type is ClaimTypes.NameIdentifier
		var sut = CreateSut(userId, useSub: false);

		// Act
		var result = await sut.VoteAsync(issueId);

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(
			Arg.Is<VoteIssueCommand>(c => c.UserId == userId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_PassCancellationToken_To_MediatorOnVote()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var token = cts.Token;
		var issueDto = CreateTestIssueDto();

		_mediator.Send(Arg.Any<VoteIssueCommand>(), token)
			.Returns(Result.Ok(issueDto));

		var sut = CreateSut("user-ct");

		// Act
		await sut.VoteAsync("issue-1", token);

		// Assert
		await _mediator.Received(1).Send(Arg.Any<VoteIssueCommand>(), token);
	}

	#endregion

	// -----------------------------------------------------------------------
	// VoteAsync — authentication failures
	// -----------------------------------------------------------------------

	#region VoteAsync Auth Failures

	[Fact]
	public async Task Should_ReturnFailure_When_UserNotAuthenticated_OnVote()
	{
		// Arrange — no HttpContext (userId = null)
		var sut = CreateSut(userId: null);

		// Act
		var result = await sut.VoteAsync("issue-1");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not authenticated");
	}

	[Fact]
	public async Task Should_ReturnValidationErrorCode_When_UserNotAuthenticated_OnVote()
	{
		// Arrange
		var sut = CreateSut(userId: null);

		// Act
		var result = await sut.VoteAsync("issue-1");

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task Should_NotCallMediator_When_UserNotAuthenticated_OnVote()
	{
		// Arrange
		var sut = CreateSut(userId: null);

		// Act
		await sut.VoteAsync("issue-1");

		// Assert
		await _mediator.DidNotReceive().Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_ReturnFailure_When_HttpContextExistsButHasNoSubOrNameIdentifierClaim_OnVote()
	{
		// Arrange — real-world case: token present but missing required claim type
		var sut = CreateSutWithContextButNoClaims();

		// Act
		var result = await sut.VoteAsync("issue-1");

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await _mediator.DidNotReceive().Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>());
	}

	#endregion

	// -----------------------------------------------------------------------
	// VoteAsync — domain / mediator failures
	// -----------------------------------------------------------------------

	#region VoteAsync Domain Failures

	[Fact]
	public async Task Should_ReturnFailure_When_IssueNotFound_OnVote()
	{
		// Arrange
		_mediator.Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Issue not found", ResultErrorCode.NotFound));

		var sut = CreateSut("user-1");

		// Act
		var result = await sut.VoteAsync("nonexistent-issue");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task Should_PreventDuplicateVote_When_UserAlreadyVoted()
	{
		// Arrange — mediator returns "Already voted" (duplicate prevention in domain handler)
		_mediator.Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Already voted", ResultErrorCode.Validation));

		var sut = CreateSut("user-1");

		// Act
		var result = await sut.VoteAsync("issue-1");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Already voted");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task Should_ReturnFailure_When_MediatorReturnsExternalServiceError_OnVote()
	{
		// Arrange — simulate infrastructure/DB failure reported as a Result error
		_mediator.Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Failed to save vote", ResultErrorCode.ExternalService));

		var sut = CreateSut("user-1");

		// Act
		var result = await sut.VoteAsync("issue-1");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to save vote");
		result.ErrorCode.Should().Be(ResultErrorCode.ExternalService);
	}

	#endregion

	// -----------------------------------------------------------------------
	// UnvoteAsync — happy paths
	// -----------------------------------------------------------------------

	#region UnvoteAsync Happy Paths

	[Fact]
	public async Task Should_RemoveVote_When_UserHasPreviouslyVoted()
	{
		// Arrange
		const string issueId = "issue-abc";
		const string userId = "user-1";
		var issueDto = CreateTestIssueDto(votes: 0, votedBy: []);

		_mediator.Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		var sut = CreateSut(userId);

		// Act
		var result = await sut.UnvoteAsync(issueId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Votes.Should().Be(0);
	}

	[Fact]
	public async Task Should_SendUnvoteIssueCommand_With_CorrectIssueIdAndUserId()
	{
		// Arrange
		const string issueId = "issue-xyz";
		const string userId = "user-42";
		var issueDto = CreateTestIssueDto();

		_mediator.Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		var sut = CreateSut(userId);

		// Act
		await sut.UnvoteAsync(issueId);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<UnvoteIssueCommand>(c =>
				c.IssueId == issueId &&
				c.UserId == userId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_RemoveVote_When_UserIdResolvedFromNameIdentifierClaim()
	{
		// Arrange
		const string userId = "user-ni";
		var issueDto = CreateTestIssueDto(votes: 0, votedBy: []);

		_mediator.Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		var sut = CreateSut(userId, useSub: false);

		// Act
		var result = await sut.UnvoteAsync("issue-ni");

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(
			Arg.Is<UnvoteIssueCommand>(c => c.UserId == userId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_PassCancellationToken_To_MediatorOnUnvote()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var token = cts.Token;
		var issueDto = CreateTestIssueDto();

		_mediator.Send(Arg.Any<UnvoteIssueCommand>(), token)
			.Returns(Result.Ok(issueDto));

		var sut = CreateSut("user-ct");

		// Act
		await sut.UnvoteAsync("issue-1", token);

		// Assert
		await _mediator.Received(1).Send(Arg.Any<UnvoteIssueCommand>(), token);
	}

	#endregion

	// -----------------------------------------------------------------------
	// UnvoteAsync — authentication failures
	// -----------------------------------------------------------------------

	#region UnvoteAsync Auth Failures

	[Fact]
	public async Task Should_ReturnFailure_When_UserNotAuthenticated_OnUnvote()
	{
		// Arrange
		var sut = CreateSut(userId: null);

		// Act
		var result = await sut.UnvoteAsync("issue-1");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not authenticated");
	}

	[Fact]
	public async Task Should_ReturnValidationErrorCode_When_UserNotAuthenticated_OnUnvote()
	{
		// Arrange
		var sut = CreateSut(userId: null);

		// Act
		var result = await sut.UnvoteAsync("issue-1");

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task Should_NotCallMediator_When_UserNotAuthenticated_OnUnvote()
	{
		// Arrange
		var sut = CreateSut(userId: null);

		// Act
		await sut.UnvoteAsync("issue-1");

		// Assert
		await _mediator.DidNotReceive().Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_ReturnFailure_When_HttpContextExistsButHasNoSubOrNameIdentifierClaim_OnUnvote()
	{
		// Arrange — token present but missing required claim type
		var sut = CreateSutWithContextButNoClaims();

		// Act
		var result = await sut.UnvoteAsync("issue-1");

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await _mediator.DidNotReceive().Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>());
	}

	#endregion

	// -----------------------------------------------------------------------
	// UnvoteAsync — domain / mediator failures
	// -----------------------------------------------------------------------

	#region UnvoteAsync Domain Failures

	[Fact]
	public async Task Should_ReturnFailure_When_IssueNotFound_OnUnvote()
	{
		// Arrange
		_mediator.Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Issue not found", ResultErrorCode.NotFound));

		var sut = CreateSut("user-1");

		// Act
		var result = await sut.UnvoteAsync("nonexistent-issue");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task Should_ReturnFailure_When_UserHasNotVoted_OnUnvote()
	{
		// Arrange — toggling off a vote that doesn't exist
		_mediator.Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Not voted", ResultErrorCode.Validation));

		var sut = CreateSut("user-1");

		// Act
		var result = await sut.UnvoteAsync("issue-1");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Not voted");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task Should_ReturnFailure_When_MediatorFails_OnUnvote()
	{
		// Arrange
		_mediator.Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Failed to remove vote", ResultErrorCode.ExternalService));

		var sut = CreateSut("user-1");

		// Act
		var result = await sut.UnvoteAsync("issue-1");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to remove vote");
	}

	#endregion

	// -----------------------------------------------------------------------
	// Toggle (vote → unvote → vote round-trip contract)
	// -----------------------------------------------------------------------

	#region Toggle Logic

	[Fact]
	public async Task Should_AllowVoteAfterUnvote_When_UserTogglesVote()
	{
		// Arrange — first unvote, then vote again
		const string issueId = "issue-toggle";
		const string userId = "user-toggle";

		var afterUnvote = CreateTestIssueDto(votes: 0, votedBy: []);
		var afterRevote = CreateTestIssueDto(votes: 1, votedBy: [userId]);

		_mediator.Send(Arg.Any<UnvoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(afterUnvote));
		_mediator.Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(afterRevote));

		var sut = CreateSut(userId);

		// Act
		var unvoteResult = await sut.UnvoteAsync(issueId);
		var revoteResult = await sut.VoteAsync(issueId);

		// Assert
		unvoteResult.Success.Should().BeTrue();
		unvoteResult.Value!.Votes.Should().Be(0);

		revoteResult.Success.Should().BeTrue();
		revoteResult.Value!.Votes.Should().Be(1);
	}

	[Fact]
	public async Task Should_RejectSecondVote_Without_Unvoting_First()
	{
		// Arrange — vote twice without unvoting = domain rejects duplicate
		_mediator.Send(Arg.Any<VoteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(
				Result.Ok(CreateTestIssueDto(votes: 1)),
				Result.Fail<IssueDto>("Already voted", ResultErrorCode.Validation));

		var sut = CreateSut("user-1");

		// Act
		var first = await sut.VoteAsync("issue-1");
		var second = await sut.VoteAsync("issue-1");

		// Assert
		first.Success.Should().BeTrue();
		second.Success.Should().BeFalse();
		second.Error.Should().Contain("Already voted");
	}

	#endregion
}
