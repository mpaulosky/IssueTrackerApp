// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AdminPageTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

// Note: These tests require PLAYWRIGHT_TEST_ADMIN_EMAIL and PLAYWRIGHT_TEST_ADMIN_PASSWORD
// environment variables set with valid Auth0 credentials for an Admin-role account.
// Tests skip gracefully when admin credentials are absent.

using AppHost.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Playwright;

namespace AppHost.Tests;

/// <summary>
/// Playwright E2E tests for the Admin section of the Web application.
/// All tests require an Admin-role Auth0 account and skip gracefully when
/// <c>PLAYWRIGHT_TEST_ADMIN_EMAIL</c> / <c>PLAYWRIGHT_TEST_ADMIN_PASSWORD</c> are not set.
/// </summary>
public class AdminPageTests : BasePlaywrightTests
{
	public AdminPageTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task AdminDashboard_LoadsWithoutRedirect()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/admin");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — should stay on /admin, not redirect to login
			page.Url.Should().Contain("/admin");
		});
	}

	[Fact]
	public async Task AdminDashboard_ShowsAdminHeading()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/admin");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert
			var heading = page.Locator("h1");
			await heading.WaitForAsync();
			var text = await heading.InnerTextAsync();
			text.Should().NotBeNullOrWhiteSpace();
		});
	}

	[Fact]
	public async Task AdminCategories_LoadsForAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/admin/categories");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — accessible (not redirected to login or access-denied)
			page.Url.Should().Contain("/admin/categories");
		});
	}

	[Fact]
	public async Task AdminStatuses_LoadsForAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/admin/statuses");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert
			page.Url.Should().Contain("/admin/statuses");
		});
	}

	[Fact]
	public async Task AdminPage_RedirectsNonAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act — regular User-role account navigates to /admin
			await page.GotoAsync("/admin");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — should be redirected away (to login, home, or access-denied)
			page.Url.Should().NotContain("/admin",
				"a non-admin user should not be able to access the admin section");
		});
	}
}
