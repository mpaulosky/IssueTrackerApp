// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     NavMenuComponentTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Layout;

namespace Web.Tests.Bunit.Layout;

/// <summary>
///   Tests for NavMenuComponent role-based navigation rendering.
/// </summary>
public class NavMenuComponentTests : BunitTestBase
{
	[Fact]
	public void NavMenu_RendersWithoutErrors()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void NavMenu_RendersNavElement()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		var nav = cut.Find("nav");
		nav.Should().NotBeNull();
		nav.GetAttribute("aria-label").Should().Be("Main navigation");
	}

	[Fact]
	public void NavMenu_ForAuthenticatedUser_ShowsUserNavLinks()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().Contain("Home");
		cut.Markup.Should().Contain("Dashboard");
		cut.Markup.Should().Contain("Issues");
		cut.Markup.Should().Contain("Create");
	}

	[Fact]
	public void NavMenu_ForAuthenticatedUser_HasCorrectHrefs()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().Contain("href=\"/\"");
		cut.Markup.Should().Contain("href=\"/dashboard\"");
		cut.Markup.Should().Contain("href=\"/issues\"");
		cut.Markup.Should().Contain("href=\"/issues/create\"");
	}

	[Fact]
	public void NavMenu_ForRegularUser_DoesNotShowAdminLinks()
	{
		// Arrange — standard user, not admin
		SetupAuthenticatedUser(isAdmin: false);

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().NotContain("href=\"/admin\"");
		cut.Markup.Should().NotContain("href=\"/admin/categories\"");
		cut.Markup.Should().NotContain("href=\"/admin/statuses\"");
		cut.Markup.Should().NotContain("href=\"/admin/analytics\"");
	}

	[Fact]
	public void NavMenu_ForAdminUser_ShowsAdminLinks()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().Contain("Admin");
		cut.Markup.Should().Contain("Categories");
		cut.Markup.Should().Contain("Statuses");
		cut.Markup.Should().Contain("Analytics");
	}

	[Fact]
	public void NavMenu_ForAdminUser_HasCorrectAdminHrefs()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().Contain("href=\"/admin\"");
		cut.Markup.Should().Contain("href=\"/admin/categories\"");
		cut.Markup.Should().Contain("href=\"/admin/statuses\"");
		cut.Markup.Should().Contain("href=\"/admin/analytics\"");
	}

	[Fact]
	public void NavMenu_ForAdminUser_ShowsAdminSeparator()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert — admin section has a visual separator
		cut.Markup.Should().Contain("bg-neutral-300",
			"admin section should have a separator line");
	}

	[Fact]
	public void NavMenu_ForAnonymousUser_RendersEmptyNav()
	{
		// Arrange — use a fresh auth context (no prior policies)
		SetupAnonymousUser();

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert — nav element renders but admin links are not visible
		var nav = cut.Find("nav");
		nav.Should().NotBeNull();
		cut.Markup.Should().NotContain("href=\"/admin\"",
			"anonymous users should not see admin navigation");
	}

	[Fact]
	public void NavMenu_UsesThemeColors()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert — nav links use primary theme colors for active state
		cut.Markup.Should().Contain("bg-primary-100",
			"active nav links should use primary theme color");
		cut.Markup.Should().Contain("text-primary-700",
			"active nav links should use primary text color");
	}

	[Fact]
	public void NavMenu_NavLinksHaveTransitionEffects()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().Contain("transition-colors",
			"nav links should have smooth color transitions");
	}

	[Fact]
	public void NavMenu_IsHiddenOnMobileByDefault()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		var nav = cut.Find("nav");
		var classes = nav.GetAttribute("class") ?? "";
		classes.Should().Contain("hidden md:flex",
			"nav should be hidden on mobile and shown on medium+ screens");
	}

	[Fact]
	public void NavMenu_WithAdminRole_RendersAdminNavLink()
	{
		// Arrange — explicitly set ClaimTypes.Role = "Admin" via isAdmin helper
		SetupAuthenticatedUser(isAdmin: true);

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().Contain("href=\"/admin\"",
			"user with Admin role should see /admin nav link");
	}

	[Fact]
	public void NavMenu_WithUserRoleOnly_DoesNotRenderAdminNavLink()
	{
		// Arrange — standard user with ClaimTypes.Role = "User" only
		SetupAuthenticatedUser(isAdmin: false);

		// Act
		var cut = Render<NavMenuComponent>();

		// Assert
		cut.Markup.Should().NotContain("href=\"/admin\"",
			"user with only User role should not see /admin nav link");
	}
}
