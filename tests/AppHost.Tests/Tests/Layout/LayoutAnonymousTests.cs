// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LayoutAnonymousTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Playwright;

namespace AppHost.Tests;

/// <summary>
/// Playwright E2E tests for the Web application layout visible to anonymous (unauthenticated) users.
/// </summary>
public class LayoutAnonymousTests : BasePlaywrightTests
{
	public LayoutAnonymousTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task Layout_Header_ShowsBrandLink()
	{
		// Arrange
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var brandLink = page.Locator("header a[href=\"/\"]");
			await brandLink.WaitForAsync();

			// Assert
			var text = await brandLink.TextContentAsync();
			text.Should().Contain("IssueTracker");
		});
	}

	[Fact]
	public async Task Layout_Header_ShowsLoginLinkWhenNotAuthenticated()
	{
		// Arrange
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var loginLink = page.Locator("a[href*=\"/account/login\"]");
			await loginLink.WaitForAsync();

			// Assert
			var isVisible = await loginLink.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task Layout_NavMenu_IsHiddenWhenNotAuthenticated()
	{
		// Arrange
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var navCount = await page.Locator("nav[aria-label=\"Main navigation\"]").CountAsync();

			// Assert
			navCount.Should().Be(0);
		});
	}

	[Fact]
	public async Task Layout_Footer_ShowsCopyrightText()
	{
		// Arrange
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var footer = page.Locator("footer[role=\"contentinfo\"]");
			await footer.WaitForAsync();

			// Assert
			var text = await footer.TextContentAsync();
			text.Should().Contain("IssueTracker");
		});
	}

	[Fact]
	public async Task Layout_ThemeToggleButton_IsVisible()
	{
		// Arrange
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var toggleBtn = page.Locator("button[aria-label=\"Toggle theme\"]");
			await toggleBtn.WaitForAsync();

			// Assert
			var isVisible = await toggleBtn.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task Layout_ColorSchemeButton_IsVisible()
	{
		// Arrange
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var schemeBtn = page.Locator("button[aria-label=\"Change color scheme\"]");
			await schemeBtn.WaitForAsync();

			// Assert
			var isVisible = await schemeBtn.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}
}
