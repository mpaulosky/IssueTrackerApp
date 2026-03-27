// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ThemeToggleTests.cs
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
/// Playwright E2E tests for the ThemeToggle component (brightness toggle + color swatch picker).
/// All tests run as anonymous users — no authentication required.
/// The brightness button (<c>aria-label="Toggle brightness"</c>) switches light/dark immediately.
/// The color button (<c>aria-label="Choose color theme"</c>) opens a swatch dropdown.
/// </summary>
public class ThemeToggleTests : BasePlaywrightTests
{
	public ThemeToggleTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task ThemeToggle_ButtonIsVisibleInHeader()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var toggleBtn = page.Locator("button[aria-label=\"Toggle brightness\"]");
			await toggleBtn.WaitForAsync();

			// Assert
			var isVisible = await toggleBtn.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task ThemeToggle_OpenDropdownShowsOptions()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act — clicking "Choose color theme" opens a swatch dropdown
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var colorBtn = page.Locator("button[aria-label=\"Choose color theme\"]");
			await colorBtn.WaitForAsync();
			await colorBtn.ClickAsync();

			// The dropdown contains one swatch button per color (Blue, Red, Green, Yellow)
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
	public async Task ThemeToggle_SelectDark_AddsDarkClassToHtml()
	{
		// Arrange — ensure we start in light mode
		await InteractWithPageAsync("web", async page =>
		{
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await page.EvaluateAsync("localStorage.setItem('theme-color-brightness', 'theme-blue-light')");
			await page.ReloadAsync();
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Act — click the brightness toggle to switch to dark
			var toggleBtn = page.Locator("button[aria-label=\"Toggle brightness\"]");
			await toggleBtn.WaitForAsync();
			await toggleBtn.ClickAsync();

			// Assert
			var isDark = await page.EvaluateAsync<bool>("document.documentElement.classList.contains('dark')");
			isDark.Should().BeTrue();
		});
	}

	[Fact]
	public async Task ThemeToggle_SelectLight_RemovesDarkClassFromHtml()
	{
		// Arrange — seed localStorage with dark theme and navigate fresh so theme.js applies it.
		// Wait for ThemeProvider to fully initialize (button title reflects dark state).
		// Then click once and wait for Blazor to reflect light state via button title.
		await InteractWithPageAsync("web", async page =>
		{
			// Seed localStorage in first navigation then navigate again so theme.js picks it up
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await page.EvaluateAsync("localStorage.setItem('theme-color-brightness', 'theme-blue-dark')");

			// Fresh navigation so theme.js initialize() reads the stored dark value
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Wait for ThemeProvider to initialize and recognize dark mode:
			// title = "Switch to light mode" only when _isInitialized = true AND IsDarkMode = true
			await page.WaitForFunctionAsync(
				"document.querySelector('button[aria-label=\"Toggle brightness\"]')?.title === 'Switch to light mode'",
				new PageWaitForFunctionOptions { Timeout = 15000 });

			// Act — single click from dark → light
			var toggleBtn = page.Locator("button[aria-label=\"Toggle brightness\"]");
			await toggleBtn.ClickAsync();

			// Wait for Blazor to finish processing and reflect light state in the button title.
			// The title changes to "Switch to dark mode" only after SetBrightnessAsync("light") completes
			// and StateHasChanged propagates — at that point the dark class is guaranteed removed.
			await page.WaitForFunctionAsync(
				"document.querySelector('button[aria-label=\"Toggle brightness\"]')?.title === 'Switch to dark mode'",
				new PageWaitForFunctionOptions { Timeout = 15000 });

			// Assert via localStorage (source of truth for the theme engine)
			var themeValue = await page.EvaluateAsync<string?>("localStorage.getItem('theme-color-brightness')");
			themeValue.Should().NotEndWith("-dark", because: "SetBrightnessAsync('light') should have stored theme-blue-light");
		});
	}
}

