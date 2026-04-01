// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementExtensions.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using Domain.Features.Admin.Abstractions;

namespace Web.Features.Admin.Users;

/// <summary>
///   Extension methods for registering Auth0 user-management services into the DI container.
/// </summary>
public static class UserManagementExtensions
{
	/// <summary>
	///   Registers <see cref="IUserManagementService" /> and its supporting infrastructure.
	/// </summary>
	/// <param name="services">The application's service collection.</param>
	/// <param name="configuration">The application configuration (reads <c>Auth0Management</c> section).</param>
	/// <returns>The same <see cref="IServiceCollection" /> for chaining.</returns>
	public static IServiceCollection AddUserManagement(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Bind and validate options from the Auth0Management config section.
		services.Configure<Auth0ManagementOptions>(
			configuration.GetSection(Auth0ManagementOptions.SectionName));

		// Register the service as scoped — a new instance per HTTP request.
		services.AddScoped<IUserManagementService, UserManagementService>();

		return services;
	}
}
