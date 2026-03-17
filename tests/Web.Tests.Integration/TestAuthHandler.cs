// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     TestAuthHandler.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Tests.Integration;

/// <summary>
/// Custom authentication handler for integration tests.
/// Returns an authenticated ClaimsPrincipal with configurable claims.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	/// <summary>
	/// The authentication scheme name used by this handler.
	/// </summary>
	public const string SchemeName = "TestScheme";

	/// <summary>
	/// Default test user ID.
	/// </summary>
	public const string TestUserId = "auth0|test-user-id-12345";

	/// <summary>
	/// Default test user email.
	/// </summary>
	public const string TestUserEmail = "testuser@example.com";

	/// <summary>
	/// Default test user name.
	/// </summary>
	public const string TestUserName = "Test User";

	public TestAuthHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder) : base(options, logger, encoder)
	{
	}

	/// <summary>
	/// Authenticates the request by returning a test user with default claims.
	/// </summary>
	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		// Check if authentication should be skipped (anonymous request)
		if (Context.Request.Headers.TryGetValue("X-Test-Anonymous", out var anonymous) &&
				anonymous.ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, TestUserId),
			new(ClaimTypes.Name, TestUserName),
			new(ClaimTypes.Email, TestUserEmail),
			new("sub", TestUserId)
		};

		// Add custom roles from header if provided
		if (Context.Request.Headers.TryGetValue("X-Test-Role", out var roleHeader))
		{
			var roles = roleHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
			foreach (var role in roles)
			{
				claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
			}
		}
		else
		{
			// Default to User role
			claims.Add(new Claim(ClaimTypes.Role, "User"));
		}

		// Add custom user ID from header if provided
		if (Context.Request.Headers.TryGetValue("X-Test-UserId", out var userIdHeader))
		{
			// Replace the default user ID claim
			claims.RemoveAll(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
			claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdHeader.ToString()));
			claims.Add(new Claim("sub", userIdHeader.ToString()));
		}

		var identity = new ClaimsIdentity(claims, SchemeName);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, SchemeName);

		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}

/// <summary>
/// Extension methods for configuring test authentication.
/// </summary>
public static class TestAuthExtensions
{
	/// <summary>
	/// Adds test authentication to the service collection.
	/// </summary>
	public static IServiceCollection AddTestAuthentication(this IServiceCollection services)
	{
		services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
				options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
				options.DefaultScheme = TestAuthHandler.SchemeName;
			})
			.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
				TestAuthHandler.SchemeName, _ => { });

		return services;
	}
}
