// Copyright (c) 2026. All rights reserved.

using Domain.Abstractions;
using Domain.DTOs;

namespace Web.Services;

/// <summary>
///   Service for voting on issues.
/// </summary>
public interface IVotingService
{
	/// <summary>
	///   Casts a vote for the current user on the specified issue.
	/// </summary>
	Task<Result<IssueDto>> VoteAsync(string issueId, CancellationToken ct = default);

	/// <summary>
	///   Removes the current user's vote from the specified issue.
	/// </summary>
	Task<Result<IssueDto>> UnvoteAsync(string issueId, CancellationToken ct = default);
}
