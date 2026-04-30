// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementExtensions.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using Auth0.ManagementApi;

using Domain.Features.Admin.Abstractions;

using Microsoft.Extensions.Options;

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
		// Ensure IMemoryCache is available (idempotent — safe to call multiple times).
		services.AddMemoryCache();

		// Bind and validate options from the Auth0Management config section.
		services.Configure<Auth0ManagementOptions>(
			configuration.GetSection(Auth0ManagementOptions.SectionName));

		// Register the Auth0 management client as a singleton; the SDK's
		// ClientCredentialsTokenProvider handles M2M token acquisition and caching internally.
		services.AddSingleton<IManagementApiClient>(sp =>
		{
			var opts = sp.GetRequiredService<IOptions<Auth0ManagementOptions>>().Value;
			var audience = string.IsNullOrWhiteSpace(opts.Audience) ? null : opts.Audience;

			return new ManagementClient(new ManagementClientOptions
			{
				Domain = opts.Domain,
				TokenProvider = new ClientCredentialsTokenProvider(
					opts.Domain,
					opts.ClientId,
					opts.ClientSecret,
					audience: audience)
			});
		});

		// Register the service as scoped — a new instance per HTTP request.
		services.AddScoped<IUserManagementService, UserManagementService>();

		return services;
	}
}
