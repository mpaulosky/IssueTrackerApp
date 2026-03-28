// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ColorSchemeTests.cs
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
/// Playwright E2E tests for the color-scheme swatch picker in the ThemeToggle header control.
/// All tests run as anonymous users — no authentication required.
/// Theme is applied as a CSS class on <c>&lt;html&gt;</c> (e.g., <c>theme-blue-light</c>),
/// stored in localStorage under the key <c>theme-color-brightness</c>.
/// </summary>
public class ColorSchemeTests : BasePlaywrightTests
{
	public ColorSchemeTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task ColorScheme_ButtonIsVisibleInHeader()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var schemeBtn = page.Locator("button[aria-label=\"Choose color theme\"]");
			await schemeBtn.WaitForAsync();

			// Assert
			var isVisible = await schemeBtn.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task ColorScheme_OpenDropdownShowsColorOptions()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var schemeBtn = page.Locator("button[aria-label=\"Choose color theme\"]");
			await schemeBtn.WaitForAsync();
			await schemeBtn.ClickAsync();

			// Color swatch buttons: title="Blue theme", title="Red theme", etc.
			var blueOption = page.Locator("button[aria-label=\"Blue color theme\"]");
			var redOption = page.Locator("button[aria-label=\"Red color theme\"]");
			var greenOption = page.Locator("button[aria-label=\"Green color theme\"]");
			var yellowOption = page.Locator("button[aria-label=\"Yellow color theme\"]");

			// Assert
			await blueOption.WaitForAsync();
			(await blueOption.IsVisibleAsync()).Should().BeTrue();
			(await redOption.IsVisibleAsync()).Should().BeTrue();
			(await greenOption.IsVisibleAsync()).Should().BeTrue();
			(await yellowOption.IsVisibleAsync()).Should().BeTrue();
		});
	}

	[Fact]
	public async Task ColorScheme_SelectRed_AppliesRedTheme()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Wait for ThemeProvider to initialize — SetColorAsync checks _isInitialized and
			// returns early if called before JS interop has completed on the server.
			// data-theme-ready is set by themeManager.markInitialized() after initialization.
			await page.WaitForFunctionAsync(
				"document.documentElement.getAttribute('data-theme-ready') === 'true'",
				null,
				new PageWaitForFunctionOptions { Timeout = 30000 });

			var schemeBtn = page.Locator("button[aria-label=\"Choose color theme\"]");
			await schemeBtn.WaitForAsync();
			await schemeBtn.ClickAsync();

			var redOption = page.Locator("button[aria-label=\"Red color theme\"]");
			await redOption.WaitForAsync();
			await redOption.ClickAsync();

			// Allow Blazor Server to process the onclick event via SignalR before checking localStorage.
			// Without this, localStorage may not yet be updated when CI's SignalR is under load.
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Wait for red theme to be persisted in localStorage (source of truth)
			await page.WaitForFunctionAsync(
				"(localStorage.getItem('theme-color-brightness') || '').startsWith('theme-red-')",
				null,
				new PageWaitForFunctionOptions { Timeout = 30000 });

			// Assert via localStorage (source of truth for the theme engine)
			var themeValue = await page.EvaluateAsync<string?>("localStorage.getItem('theme-color-brightness')");
			themeValue.Should().StartWith("theme-red-");
		});
	}

	[Fact]
	public async Task ColorScheme_DefaultThemeIsBlue()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act — clear saved theme (key: 'theme-color-brightness') to get the default
			await page.GotoAsync("/");
			await page.EvaluateAsync("localStorage.removeItem('theme-color-brightness')");
			await page.ReloadAsync();
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Default is theme-blue-light (applied as a CSS class on <html>)
			var hasBlueTheme = await page.EvaluateAsync<bool>(
				"[...document.documentElement.classList].some(c => c.startsWith('theme-blue-'))");

			// Assert
			hasBlueTheme.Should().BeTrue();
		});
	}
}

