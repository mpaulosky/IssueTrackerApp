// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleAssignmentDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.DTOs.Admin;

/// <summary>
///   Data transfer object for <see cref="Domain.Features.Admin.Models.RoleAssignment" />.
/// </summary>
[Serializable]
[method: JsonConstructor]
public record RoleAssignmentDto(
	[property: JsonPropertyName("roleId")] string RoleId,
	[property: JsonPropertyName("roleName")] string RoleName);
