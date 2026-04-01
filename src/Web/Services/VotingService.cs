// Copyright (c) 2026. All rights reserved.

using System.Security.Claims;

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Issues.Commands;

using MediatR;

namespace Web.Services;

/// <summary>
///   Service facade for issue voting, wrapping MediatR calls and extracting the current user.
/// </summary>
public sealed class VotingService : IVotingService
{
	private readonly IMediator _mediator;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly ILogger<VotingService> _logger;

	public VotingService(
		IMediator mediator,
		IHttpContextAccessor httpContextAccessor,
		ILogger<VotingService> logger)
	{
		_mediator = mediator;
		_httpContextAccessor = httpContextAccessor;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task<Result<IssueDto>> VoteAsync(string issueId, CancellationToken ct = default)
	{
		var userId = GetCurrentUserId();
		if (userId is null)
		{
			return Result.Fail<IssueDto>("User is not authenticated", ResultErrorCode.Validation);
		}

		_logger.LogInformation("User {UserId} voting on issue {IssueId}", userId, issueId);
		return await _mediator.Send(new VoteIssueCommand(issueId, userId), ct);
	}

	/// <inheritdoc />
	public async Task<Result<IssueDto>> UnvoteAsync(string issueId, CancellationToken ct = default)
	{
		var userId = GetCurrentUserId();
		if (userId is null)
		{
			return Result.Fail<IssueDto>("User is not authenticated", ResultErrorCode.Validation);
		}

		_logger.LogInformation("User {UserId} removing vote from issue {IssueId}", userId, issueId);
		return await _mediator.Send(new UnvoteIssueCommand(issueId, userId), ct);
	}

	private string? GetCurrentUserId()
	{
		return _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
			?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	}
}
