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
/// Playwright E2E tests for the ThemeToggle component (light / dark / system modes).
/// All tests run as anonymous users — no authentication required.
/// </summary>
public class ThemeToggleTests : BasePlaywrightTests
{
	public ThemeToggleTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task ThemeToggle_ButtonIsVisibleInHeader()
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
	public async Task ThemeToggle_OpenDropdownShowsOptions()
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
			await toggleBtn.ClickAsync();

			var lightOption = page.Locator("button:has-text(\"Light\")");
			var darkOption = page.Locator("button:has-text(\"Dark\")");
			var systemOption = page.Locator("button:has-text(\"System\")");

			// Assert
			await lightOption.WaitForAsync();
			await darkOption.WaitForAsync();
			await systemOption.WaitForAsync();

			(await lightOption.IsVisibleAsync()).Should().BeTrue();
			(await darkOption.IsVisibleAsync()).Should().BeTrue();
			(await systemOption.IsVisibleAsync()).Should().BeTrue();
		});
	}

	[Fact]
	public async Task ThemeToggle_SelectDark_AddsDarkClassToHtml()
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
			await toggleBtn.ClickAsync();

			var darkOption = page.Locator("button:has-text(\"Dark\")");
			await darkOption.WaitForAsync();
			await darkOption.ClickAsync();

			// Assert
			var isDark = await page.EvaluateAsync<bool>("document.documentElement.classList.contains('dark')");
			isDark.Should().BeTrue();
		});
	}

	[Fact]
	public async Task ThemeToggle_SelectLight_RemovesDarkClassFromHtml()
	{
		// Arrange
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act — start in dark mode by setting localStorage and reloading
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await page.EvaluateAsync("localStorage.setItem('theme-mode', 'dark')");
			await page.ReloadAsync();
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Open dropdown and select Light
			var toggleBtn = page.Locator("button[aria-label=\"Toggle theme\"]");
			await toggleBtn.WaitForAsync();
			await toggleBtn.ClickAsync();

			var lightOption = page.Locator("button:has-text(\"Light\")");
			await lightOption.WaitForAsync();
			await lightOption.ClickAsync();

			// Assert
			var isDark = await page.EvaluateAsync<bool>("document.documentElement.classList.contains('dark')");
			isDark.Should().BeFalse();
		});
	}
}
