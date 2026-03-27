// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LayoutAdminTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

// Note: These tests require PLAYWRIGHT_TEST_ADMIN_EMAIL and PLAYWRIGHT_TEST_ADMIN_PASSWORD
// environment variables to be set with valid Auth0 credentials for an Admin-role account.
// If those env vars are not set, every test in this class skips gracefully.

using AppHost.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Playwright;

namespace AppHost.Tests;

/// <summary>
/// Playwright E2E tests for the Web application layout visible only to Admin-role users.
/// Tests are skipped automatically when admin Auth0 credentials are not configured.
/// </summary>
public class LayoutAdminTests : BasePlaywrightTests
{
	public LayoutAdminTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task Layout_AdminNav_IsVisibleForAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — admin link or admin nav section is present
			var adminLink = page.Locator("a[href^=\"/admin\"]").First;
			var isVisible = await adminLink.IsVisibleAsync();
			isVisible.Should().BeTrue("an Admin-role user should see the admin navigation link");
		});
	}

	[Fact]
	public async Task Layout_AdminNav_IsHiddenForNonAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — admin link should NOT be present for a regular User-role account
			var adminLinkCount = await page.Locator("a[href^=\"/admin\"]").CountAsync();
			adminLinkCount.Should().Be(0, "a standard User-role account should not see admin navigation");
		});
	}
}
