// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ListUsersQuery.cs
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
///   Query to retrieve a paginated list of administrative users from Auth0.
/// </summary>
public record ListUsersQuery(
	int Page,
	int PageSize,
	string? SearchTerm) : IRequest<Result<IReadOnlyList<AdminUserSummary>>>;

/// <summary>
///   Handler for <see cref="ListUsersQuery" />.
/// </summary>
public sealed class ListUsersQueryHandler
	: IRequestHandler<ListUsersQuery, Result<IReadOnlyList<AdminUserSummary>>>
{
	private readonly IUserManagementService _userManagementService;
	private readonly ILogger<ListUsersQueryHandler> _logger;

	public ListUsersQueryHandler(
		IUserManagementService userManagementService,
		ILogger<ListUsersQueryHandler> logger)
	{
		_userManagementService = userManagementService;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<AdminUserSummary>>> Handle(
		ListUsersQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation(
			"Listing users — Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
			request.Page,
			request.PageSize,
			request.SearchTerm ?? "(none)");

		var result = await _userManagementService.ListUsersAsync(
			request.Page,
			request.PageSize,
			cancellationToken);

		if (result.Failure)
		{
			_logger.LogError("Failed to list users: {Error}", result.Error);
			return Result.Fail<IReadOnlyList<AdminUserSummary>>(
				result.Error ?? "Failed to list users",
				result.ErrorCode);
		}

		var users = result.Value ?? [];

		if (!string.IsNullOrWhiteSpace(request.SearchTerm))
		{
			var term = request.SearchTerm.Trim();
			users = users
				.Where(u =>
					u.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					u.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
				.ToList()
				.AsReadOnly();
		}

		_logger.LogInformation("Successfully retrieved {Count} user(s)", users.Count);
		return Result.Ok(users);
	}
}
