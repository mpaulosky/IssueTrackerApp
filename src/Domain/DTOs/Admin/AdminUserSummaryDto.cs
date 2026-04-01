// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AdminUserSummaryDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.DTOs.Admin;

/// <summary>
///   Data transfer object for <see cref="Domain.Features.Admin.Models.AdminUserSummary" />.
/// </summary>
[Serializable]
[method: JsonConstructor]
public record AdminUserSummaryDto(
	[property: JsonPropertyName("userId")] string UserId,
	[property: JsonPropertyName("email")] string Email,
	[property: JsonPropertyName("name")] string Name,
	[property: JsonPropertyName("picture")] string Picture,
	[property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
	[property: JsonPropertyName("lastLogin")] DateTimeOffset? LastLogin,
	[property: JsonPropertyName("isBlocked")] bool IsBlocked);
