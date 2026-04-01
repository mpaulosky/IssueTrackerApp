// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GetUserByIdQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

using Domain.Abstractions;
using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

namespace Domain.Features.Admin.Users.Queries;

/// <summary>
///   Query to retrieve a single administrative user by their Auth0 identifier.
/// </summary>
public record GetUserByIdQuery(string UserId) : IRequest<Result<AdminUserSummary>>;

/// <summary>
///   Handler for <see cref="GetUserByIdQuery" />.
/// </summary>
public sealed class GetUserByIdQueryHandler
	: IRequestHandler<GetUserByIdQuery, Result<AdminUserSummary>>
{
	private readonly IUserManagementService _userManagementService;
	private readonly ILogger<GetUserByIdQueryHandler> _logger;

	public GetUserByIdQueryHandler(
		IUserManagementService userManagementService,
		ILogger<GetUserByIdQueryHandler> logger)
	{
		_userManagementService = userManagementService;
		_logger = logger;
	}

	public async Task<Result<AdminUserSummary>> Handle(
		GetUserByIdQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Fetching user with ID: {UserId}", request.UserId);

		var result = await _userManagementService.GetUserByIdAsync(request.UserId, cancellationToken);

		if (result.Failure || result.Value is null)
		{
			_logger.LogWarning("User not found with ID: {UserId}", request.UserId);
			return Result.Fail<AdminUserSummary>(
				result.Error ?? "User not found",
				result.ErrorCode == ResultErrorCode.None ? ResultErrorCode.NotFound : result.ErrorCode);
		}

		_logger.LogInformation("Successfully fetched user with ID: {UserId}", request.UserId);
		return Result.Ok(result.Value);
	}
}
