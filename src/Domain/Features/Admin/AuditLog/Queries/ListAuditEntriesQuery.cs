// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ListAuditEntriesQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

using Domain.Abstractions;
using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;

namespace Domain.Features.Admin.AuditLog.Queries;

/// <summary>
///   Query to retrieve all role-change audit entries for a specific target user,
///   returned in reverse-chronological order.
/// </summary>
public record ListAuditEntriesQuery(string TargetUserId)
	: IRequest<Result<IReadOnlyList<RoleChangeAuditEntry>>>;

/// <summary>
///   Handler for <see cref="ListAuditEntriesQuery" />.
/// </summary>
public sealed class ListAuditEntriesQueryHandler
	: IRequestHandler<ListAuditEntriesQuery, Result<IReadOnlyList<RoleChangeAuditEntry>>>
{
	private readonly IAuditLogRepository _auditLogRepository;
	private readonly ILogger<ListAuditEntriesQueryHandler> _logger;

	public ListAuditEntriesQueryHandler(
		IAuditLogRepository auditLogRepository,
		ILogger<ListAuditEntriesQueryHandler> logger)
	{
		_auditLogRepository = auditLogRepository;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<RoleChangeAuditEntry>>> Handle(
		ListAuditEntriesQuery request,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation(
			"Listing audit entries for target user '{TargetUserId}'",
			request.TargetUserId);

		try
		{
			var entries = await _auditLogRepository.GetByTargetUserAsync(
				request.TargetUserId,
				cancellationToken);

			var sorted = entries
				.OrderByDescending(e => e.Timestamp)
				.ToList()
				.AsReadOnly();

			_logger.LogInformation(
				"Retrieved {Count} audit entry(ies) for target user '{TargetUserId}'",
				sorted.Count,
				request.TargetUserId);

			return Result.Ok<IReadOnlyList<RoleChangeAuditEntry>>(sorted);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex,
				"Error retrieving audit entries for target user '{TargetUserId}'",
				request.TargetUserId);

			return Result.Fail<IReadOnlyList<RoleChangeAuditEntry>>(
				$"Failed to retrieve audit log: {ex.Message}");
		}
	}
}
