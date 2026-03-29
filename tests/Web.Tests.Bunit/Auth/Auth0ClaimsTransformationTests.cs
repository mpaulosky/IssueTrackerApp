// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Auth0ClaimsTransformationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using System.Security.Claims;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Web.Auth;

namespace Web.Tests.Bunit.Auth;

/// <summary>
///   Tests for Auth0ClaimsTransformation — maps Auth0 custom namespace role claims
///   to standard ClaimTypes.Role claims for ASP.NET Core authorization.
/// </summary>
public class Auth0ClaimsTransformationTests
{
	private const string TestNamespace = "https://issuetracker.com/roles";

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

	private static Auth0ClaimsTransformation CreateTransformation(string? roleClaimNamespace = TestNamespace)
	{
		var config = CreateConfiguration(roleClaimNamespace);
		var logger = NullLogger<Auth0ClaimsTransformation>.Instance;
		return new Auth0ClaimsTransformation(config, logger);
	}

	private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
	{
		var identity = new ClaimsIdentity(claims, "TestAuth");
		return new ClaimsPrincipal(identity);
	}

	[Fact]
	public async Task TransformAsync_WithSingleRole_MapsToStandardRoleClaim()
	{
		// Arrange
		var transformation = CreateTransformation();
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, "Admin"));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		result.IsInRole("Admin").Should().BeTrue();
		result.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithJsonArrayRoles_MapsAllRoles()
	{
		// Arrange
		var transformation = CreateTransformation();
		var rolesJson = JsonSerializer.Serialize(new[] { "Admin", "User" });
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, rolesJson));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		result.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
		result.HasClaim(ClaimTypes.Role, "User").Should().BeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithCommaSeparatedRoles_MapsAllRoles()
	{
		// Arrange
		var transformation = CreateTransformation();
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, "Admin,User"));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		result.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
		result.HasClaim(ClaimTypes.Role, "User").Should().BeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithNoRoleClaims_ReturnsUnmodifiedPrincipal()
	{
		// Arrange
		var transformation = CreateTransformation();
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(ClaimTypes.Name, "Test User"));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		result.HasClaim(c => c.Type == ClaimTypes.Role).Should().BeFalse();
	}

	[Fact]
	public async Task TransformAsync_WithNamespaceClaimButNoNamespaceConfig_ShouldAutoDetectViaPass3()
	{
		// Arrange — no namespace configured, but principal carries a namespaced role claim.
		// Pass 3 auto-detects claim types ending in "/roles" and maps them.
		var transformation = CreateTransformation(roleClaimNamespace: null);
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, "Admin"));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert — Pass 3 should auto-detect the "https://issuetracker.com/roles" claim
		result.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithNamespaceClaimAndEmptyNamespaceConfig_ShouldAutoDetectViaPass3()
	{
		// Arrange — empty namespace configured, but principal carries a namespaced role claim.
		// Pass 3 auto-detects claim types ending in "/roles" and maps them.
		var transformation = CreateTransformation(roleClaimNamespace: "");
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, "Admin"));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert — Pass 3 should auto-detect the "https://issuetracker.com/roles" claim
		result.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenAlreadyTransformed_DoesNotDuplicateRoles()
	{
		// Arrange
		var transformation = CreateTransformation();
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, "Admin"),
			new Claim(ClaimTypes.Role, "Admin") // Already has standard role claim
		);

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert — should not add a duplicate
		result.FindAll(c => c.Type == ClaimTypes.Role && c.Value == "Admin")
			.Should().HaveCount(1);
	}

	[Fact]
	public async Task TransformAsync_WithUnauthenticatedPrincipal_ReturnsUnmodified()
	{
		// Arrange
		var transformation = CreateTransformation();
		var identity = new ClaimsIdentity(); // No auth type → not authenticated
		var principal = new ClaimsPrincipal(identity);

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		result.HasClaim(c => c.Type == ClaimTypes.Role).Should().BeFalse();
	}

	[Fact]
	public async Task TransformAsync_WithInvalidJsonArray_HandlesGracefully()
	{
		// Arrange — value starts and ends with brackets but is not valid JSON
		var transformation = CreateTransformation();
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, "[invalid json]"));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert — should not throw, and should not add any roles from the malformed JSON
		result.HasClaim(c => c.Type == ClaimTypes.Role).Should().BeFalse();
	}

	[Fact]
	public async Task TransformAsync_WithWhitespaceInCommaSeparatedRoles_TrimsValues()
	{
		// Arrange
		var transformation = CreateTransformation();
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, " Admin , User "));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		result.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
		result.HasClaim(ClaimTypes.Role, "User").Should().BeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithMultipleNamespaceClaims_MapsAll()
	{
		// Arrange
		var transformation = CreateTransformation();
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, "Admin"),
			new Claim(TestNamespace, "User"));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert
		result.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
		result.HasClaim(ClaimTypes.Role, "User").Should().BeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithPartialExistingRoles_AddsOnlyMissingRoles()
	{
		// Arrange — principal already has "Admin" role but Auth0 sends both "Admin" and "User"
		var transformation = CreateTransformation();
		var principal = CreatePrincipal(
			new Claim(ClaimTypes.NameIdentifier, "user123"),
			new Claim(TestNamespace, "[\"Admin\",\"User\"]"),
			new Claim(ClaimTypes.Role, "Admin"));

		// Act
		var result = await transformation.TransformAsync(principal);

		// Assert — "Admin" should not be duplicated, "User" should be added
		result.FindAll(c => c.Type == ClaimTypes.Role && c.Value == "Admin")
			.Should().HaveCount(1);
		result.HasClaim(ClaimTypes.Role, "User").Should().BeTrue();
	}
}
