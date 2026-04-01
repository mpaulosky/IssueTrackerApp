// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RemoveRoleCommandValidator.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Features.Admin.Users.Commands;

/// <summary>
///   Validator for <see cref="RemoveRoleCommand" />.
/// </summary>
public sealed class RemoveRoleCommandValidator : AbstractValidator<RemoveRoleCommand>
{
	public RemoveRoleCommandValidator()
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
