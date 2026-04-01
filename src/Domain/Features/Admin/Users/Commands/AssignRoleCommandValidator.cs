// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AssignRoleCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Features.Admin.Users.Commands;

/// <summary>
///   Validator for <see cref="AssignRoleCommand" />.
/// </summary>
public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
	public AssignRoleCommandValidator()
	{
		RuleFor(x => x.AdminUserId)
			.NotEmpty()
			.WithMessage("Admin user ID is required");

		RuleFor(x => x.AdminUserName)
			.NotEmpty()
			.WithMessage("Admin user name is required");

		RuleFor(x => x.TargetUserId)
			.NotEmpty()
			.WithMessage("Target user ID is required");

		RuleFor(x => x.RoleName)
			.NotEmpty()
			.WithMessage("Role name is required");
	}
}
