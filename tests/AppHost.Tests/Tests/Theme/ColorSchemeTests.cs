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
/// Playwright E2E tests for the ColorSchemeSelector component (blue / red / green / yellow).
/// All tests run as anonymous users — no authentication required.
/// </summary>
public class ColorSchemeTests : BasePlaywrightTests
{
	public ColorSchemeTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task ColorScheme_ButtonIsVisibleInHeader()
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

	[Fact]
	public async Task ColorScheme_OpenDropdownShowsColorOptions()
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
			await schemeBtn.ClickAsync();

			var blueOption = page.Locator("button[title=\"Blue\"]");
			var redOption = page.Locator("button[title=\"Red\"]");
			var greenOption = page.Locator("button[title=\"Green\"]");
			var yellowOption = page.Locator("button[title=\"Yellow\"]");

			// Assert
			await blueOption.WaitForAsync();
			await redOption.WaitForAsync();
			await greenOption.WaitForAsync();
			await yellowOption.WaitForAsync();

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
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var schemeBtn = page.Locator("button[aria-label=\"Change color scheme\"]");
			await schemeBtn.WaitForAsync();
			await schemeBtn.ClickAsync();

			var redOption = page.Locator("button[title=\"Red\"]");
			await redOption.WaitForAsync();
			await redOption.ClickAsync();

			// Assert
			var theme = await page.EvaluateAsync<string>("document.documentElement.getAttribute('data-theme')");
			theme.Should().Be("red");
		});
	}

	[Fact]
	public async Task ColorScheme_DefaultThemeIsBlue()
	{
		// Arrange
		await ConfigureAsync<Projects.AppHost>();

		await InteractWithPageAsync("web", async page =>
		{
			// Act — navigate fresh (clear localStorage so we get the default)
			await page.GotoAsync("/");
			await page.EvaluateAsync("localStorage.removeItem('color-scheme')");
			await page.ReloadAsync();
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert
			var theme = await page.EvaluateAsync<string>("document.documentElement.getAttribute('data-theme')");
			theme.Should().Be("blue");
		});
	}
}
