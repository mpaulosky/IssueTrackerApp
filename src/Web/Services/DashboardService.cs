// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DashboardService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Features.Dashboard.Queries;

using MediatR;

namespace Web.Services;

/// <summary>
///   Service facade for Dashboard operations, wrapping MediatR calls.
/// </summary>
public interface IDashboardService
{
	/// <summary>
	///   Gets dashboard data for the specified user.
	/// </summary>
	Task<Result<UserDashboardDto>> GetUserDashboardAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
///   Implementation of IDashboardService using MediatR.
/// </summary>
public sealed class DashboardService : IDashboardService
{
	private readonly IMediator _mediator;

	public DashboardService(IMediator mediator)
	{
		_mediator = mediator;
	}

	public async Task<Result<UserDashboardDto>> GetUserDashboardAsync(
		string userId,
		CancellationToken cancellationToken = default)
	{
		var query = new GetUserDashboardQuery(userId);
		return await _mediator.Send(query, cancellationToken);
	}
}
