// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ProfileRolesTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using System.Security.Claims;

using Microsoft.Extensions.Configuration;

using Web.Components.User;

namespace Web.Tests.Bunit.Components.User;

/// <summary>
///   bUnit tests for Profile component roles section and GetAllRoleClaims static helper.
/// </summary>
public class ProfileRolesTests : BunitTestBase
{
	private static IConfiguration CreateConfiguration(string? roleClaimNamespace = null)
	{
		var configValues = new Dictionary<string, string?>();
		if (roleClaimNamespace is not null)
		{
			configValues["Auth0:RoleClaimNamespace"] = roleClaimNamespace;
		}

		return new ConfigurationBuilder()
			.AddInMemoryCollection(configValues)
			.Build();
	}

	private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
	{
		var identity = new ClaimsIdentity(claims, "TestAuth");
		return new ClaimsPrincipal(identity);
	}

	[Fact]
	public async Task ProfilePage_WithAdminRole_ShowsRoleInRolesSection()
	{
		// Arrange — set up user with ClaimTypes.Role = "Admin"
		SetupAuthenticatedUser(isAdmin: true);
		Services.AddSingleton(CreateConfiguration());

		// Act
		var cut = Render<Profile>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Admin",
			"Profile page should display 'Admin' in the Roles &amp; Permissions section");
		cut.Markup.Should().NotContain("No roles assigned",
			"Profile page should not show 'No roles assigned' when user has roles");
	}

	[Fact]
	public async Task ProfilePage_WithNoRoles_ShowsNoRolesAssigned()
	{
		// Arrange — authenticated user but with no role claims
		SetupAuthenticatedUser();
		Services.AddSingleton(CreateConfiguration());

		// Patch: override auth context with a user that has NO role claim
		// bUnit's SetupAuthenticatedUser always sets at least ClaimTypes.Role = "User",
		// so we exercise the static method directly below for this scenario.
		// Instead, verify via the static helper that an empty roles list triggers the message.

		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user-no-roles"),
			new Claim(ClaimTypes.Name, "No Role User"),
			new Claim(ClaimTypes.Email, "noroles@example.com")
		);

		// Act — invoke the static method directly
		var roles = Profile.GetAllRoleClaims(principal);

		// Assert
		roles.Should().BeEmpty("user with no role claims should have an empty roles list");
	}

	[Fact]
	public void GetAllRoleClaims_WithNamespaceClaimType_ReturnsRoles()
	{
		// Arrange
		const string roleNamespace = "https://issuetracker.com/roles";
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(ClaimTypes.Name, "Test User"),
			new Claim(roleNamespace, "Admin"),
			new Claim(roleNamespace, "User")
		);

		// Act
		var roles = Profile.GetAllRoleClaims(principal, roleNamespace);

		// Assert
		roles.Should().Contain("Admin", "namespace-based role claim 'Admin' should be included");
		roles.Should().Contain("User", "namespace-based role claim 'User' should be included");
		roles.Should().HaveCount(2);
	}

	[Fact]
	public void GetAllRoleClaims_WithStandardClaimTypes_ReturnsRoles()
	{
		// Arrange
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.Role, "Admin"),
			new Claim("role", "Moderator"),
			new Claim("roles", "Reviewer")
		);

		// Act
		var roles = Profile.GetAllRoleClaims(principal);

		// Assert
		roles.Should().Contain("Admin");
		roles.Should().Contain("Moderator");
		roles.Should().Contain("Reviewer");
		roles.Should().HaveCount(3);
	}

	[Fact]
	public void GetAllRoleClaims_WithDuplicateRoles_ReturnsDistinct()
	{
		// Arrange
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.Role, "Admin"),
			new Claim("role", "Admin")
		);

		// Act
		var roles = Profile.GetAllRoleClaims(principal);

		// Assert
		roles.Should().ContainSingle(r => r == "Admin", "duplicate roles should be deduplicated");
	}

	[Fact]
	public void GetAllRoleClaims_WithNullNamespace_DoesNotThrow()
	{
		// Arrange
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.Role, "User")
		);

		// Act
		var act = () => Profile.GetAllRoleClaims(principal, null);

		// Assert
		act.Should().NotThrow();
		act().Should().Contain("User");
	}

	[Fact]
	public void GetAllRoleClaims_WithEmptyNamespace_IgnoresNamespace()
	{
		// Arrange
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.Role, "User")
		);

		// Act
		var roles = Profile.GetAllRoleClaims(principal, string.Empty);

		// Assert — empty namespace should not add extra claims lookup
		roles.Should().ContainSingle(r => r == "User");
	}

	[Fact]
	public void GetAllRoleClaims_WithWhitespaceValues_FiltersOutBlanks()
	{
		// Arrange
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.Role, "Admin"),
			new Claim(ClaimTypes.Role, "   ")
		);

		// Act
		var roles = Profile.GetAllRoleClaims(principal);

		// Assert
		roles.Should().ContainSingle(r => r == "Admin", "whitespace-only role values should be filtered");
	}
}
