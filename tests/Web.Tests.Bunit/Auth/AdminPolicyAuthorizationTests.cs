// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AdminPolicyAuthorizationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests.Bunit
// =============================================

using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Web.Auth;

namespace Web.Tests.Bunit.Auth;

/// <summary>
/// Unit tests for <c>AdminPolicy</c> authorization.
/// Verifies that the policy grants access only to principals bearing the Admin role claim,
/// using mock <see cref="ClaimsPrincipal"/> instances — no HTTP context required.
/// </summary>
public class AdminPolicyAuthorizationTests
{
	// ── Helpers ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Builds a real <see cref="IAuthorizationService"/> wired up with the same
	/// policy registrations used by the application in <c>Program.cs</c>.
	/// </summary>
	private static IAuthorizationService BuildAuthorizationService()
	{
		var services = new ServiceCollection();

		services.AddAuthorization(options =>
		{
			options.AddPolicy(AuthorizationPolicies.AdminPolicy,
				policy => policy.RequireRole(AuthorizationRoles.Admin));

			options.AddPolicy(AuthorizationPolicies.UserPolicy,
				policy => policy.RequireRole(AuthorizationRoles.User));
		});

		services.AddLogging();

		return services.BuildServiceProvider()
			.GetRequiredService<IAuthorizationService>();
	}

	/// <summary>
	/// Creates an authenticated <see cref="ClaimsPrincipal"/> with the given roles.
	/// An empty <paramref name="roles"/> array produces a principal with no role claims.
	/// </summary>
	private static ClaimsPrincipal CreatePrincipal(params string[] roles)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, "test-user"),
			new(ClaimTypes.Name,           "Test User"),
		};

		foreach (var role in roles)
		{
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
		return new ClaimsPrincipal(identity);
	}

	// ── AdminPolicy tests ─────────────────────────────────────────────────────

	[Fact]
	public async Task AdminPolicy_WhenPrincipalHasAdminRole_Succeeds()
	{
		// Arrange
		var authService = BuildAuthorizationService();
		var principal  = CreatePrincipal(AuthorizationRoles.Admin);

		// Act
		var result = await authService.AuthorizeAsync(principal, resource: null,
			policyName: AuthorizationPolicies.AdminPolicy);

		// Assert
		result.Succeeded.Should().BeTrue("a principal with the Admin role must satisfy AdminPolicy");
	}

	[Fact]
	public async Task AdminPolicy_WhenPrincipalHasAdminAndUserRoles_Succeeds()
	{
		// Arrange — having both roles must not block access
		var authService = BuildAuthorizationService();
		var principal  = CreatePrincipal(AuthorizationRoles.Admin, AuthorizationRoles.User);

		// Act
		var result = await authService.AuthorizeAsync(principal, resource: null,
			policyName: AuthorizationPolicies.AdminPolicy);

		// Assert
		result.Succeeded.Should().BeTrue("Admin+User roles must still satisfy AdminPolicy");
	}

	[Fact]
	public async Task AdminPolicy_WhenPrincipalHasUserRoleOnly_Fails()
	{
		// Arrange
		var authService = BuildAuthorizationService();
		var principal  = CreatePrincipal(AuthorizationRoles.User);

		// Act
		var result = await authService.AuthorizeAsync(principal, resource: null,
			policyName: AuthorizationPolicies.AdminPolicy);

		// Assert
		result.Succeeded.Should().BeFalse("a User-only principal must not satisfy AdminPolicy");
	}

	[Fact]
	public async Task AdminPolicy_WhenPrincipalHasNoRoles_Fails()
	{
		// Arrange
		var authService = BuildAuthorizationService();
		var principal  = CreatePrincipal(); // no roles

		// Act
		var result = await authService.AuthorizeAsync(principal, resource: null,
			policyName: AuthorizationPolicies.AdminPolicy);

		// Assert
		result.Succeeded.Should().BeFalse("a principal with no roles must not satisfy AdminPolicy");
	}

	[Fact]
	public async Task AdminPolicy_WhenPrincipalIsAnonymous_Fails()
	{
		// Arrange — unauthenticated ClaimsPrincipal (no authenticationType → IsAuthenticated = false)
		var authService = BuildAuthorizationService();
		var identity   = new ClaimsIdentity(); // no authenticationType → IsAuthenticated = false
		var principal  = new ClaimsPrincipal(identity);

		// Act
		var result = await authService.AuthorizeAsync(principal, resource: null,
			policyName: AuthorizationPolicies.AdminPolicy);

		// Assert
		result.Succeeded.Should().BeFalse("an anonymous (unauthenticated) principal must not satisfy AdminPolicy");
	}

	// ── UserPolicy sanity check ───────────────────────────────────────────────

	[Fact]
	public async Task UserPolicy_WhenPrincipalHasUserRole_Succeeds()
	{
		// Arrange — ensure UserPolicy is independent of AdminPolicy
		var authService = BuildAuthorizationService();
		var principal  = CreatePrincipal(AuthorizationRoles.User);

		// Act
		var result = await authService.AuthorizeAsync(principal, resource: null,
			policyName: AuthorizationPolicies.UserPolicy);

		// Assert
		result.Succeeded.Should().BeTrue("a principal with the User role must satisfy UserPolicy");
	}

	[Fact]
	public async Task UserPolicy_WhenPrincipalHasAdminRoleOnly_Fails()
	{
		// Arrange — Admin role must NOT automatically grant UserPolicy
		var authService = BuildAuthorizationService();
		var principal  = CreatePrincipal(AuthorizationRoles.Admin);

		// Act
		var result = await authService.AuthorizeAsync(principal, resource: null,
			policyName: AuthorizationPolicies.UserPolicy);

		// Assert
		result.Succeeded.Should().BeFalse("AdminPolicy and UserPolicy are independent — Admin role alone must not satisfy UserPolicy");
	}
}
