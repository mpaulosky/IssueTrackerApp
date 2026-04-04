// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ThemeBrightnessToggleTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Theme;

namespace Web.Tests.Bunit.Components.Theme;

/// <summary>
///   bUnit tests for the ThemeBrightnessToggleComponent.
///   The component renders a single toggle button that switches between
///   light and dark brightness.  It reads the current value from
///   themeManager.getBrightness via JSInterop on first render and writes
///   back via themeManager.setBrightness on click.
///   It does NOT use a CascadingParameter — it manages its own JSInterop.
/// </summary>
public class ThemeBrightnessToggleTests : BunitTestBase
{
	private const string ToggleButtonId = "nav-btn-brightness-toggle";
	private const string ToggleAriaLabel = "Toggle brightness";

	// -----------------------------------------------------------------------
	// Rendering
	// -----------------------------------------------------------------------

	[Fact]
	public void ThemeBrightnessToggle_RendersWithoutCrashing()
	{
		// Act
		var cut = Render<ThemeBrightnessToggleComponent>();

		// Assert – the toggle button must be in the DOM
		cut.Find($"button#{ToggleButtonId}").Should().NotBeNull(
			"the toggle button must always be rendered");
	}

	[Fact]
	public void ThemeBrightnessToggle_ToggleButton_HasCorrectAriaLabel()
	{
		// Act
		var cut = Render<ThemeBrightnessToggleComponent>();

		// Assert – assistive technology depends on this label
		var button = cut.Find($"button#{ToggleButtonId}");
		button.GetAttribute("aria-label").Should().Be(ToggleAriaLabel,
			"the button must expose an aria-label for screen-reader users");
	}

	// -----------------------------------------------------------------------
	// Icon rendering based on brightness
	// -----------------------------------------------------------------------

	[Fact]
	public void ThemeBrightnessToggle_InLightMode_ShowsMoonIcon_ToInviteDarkSwitch()
	{
		// Arrange – Loose mode: getBrightness returns null → fallback "light"
		var cut = Render<ThemeBrightnessToggleComponent>();

		// Assert – title signals the next action ("Switch to dark mode") which
		// corresponds to showing the moon icon while in light mode
		var button = cut.Find($"button#{ToggleButtonId}");
		button.GetAttribute("title").Should().Be("Switch to dark mode",
			"when brightness is light the button should invite the user to switch to dark");
	}

	[Fact]
	public void ThemeBrightnessToggle_InDarkMode_ShowsSunIcon_ToInviteLightSwitch()
	{
		// Arrange – explicit setup so getBrightness returns "dark"
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		var cut = Render<ThemeBrightnessToggleComponent>();

		// Assert
		var button = cut.Find($"button#{ToggleButtonId}");
		button.GetAttribute("title").Should().Be("Switch to light mode",
			"when brightness is dark the button should invite the user to switch to light");
	}

	// -----------------------------------------------------------------------
	// Interaction
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ThemeBrightnessToggle_Click_InLightMode_SwitchesToDark()
	{
		// Arrange – start in light mode (default)
		var cut = Render<ThemeBrightnessToggleComponent>();

		// Act
		await cut.InvokeAsync(() => cut.Find($"button#{ToggleButtonId}").Click());

		// Assert – after toggle the title should invite switching back to light
		var button = cut.Find($"button#{ToggleButtonId}");
		button.GetAttribute("title").Should().Be("Switch to light mode",
			"clicking in light mode should activate dark mode");
	}

	[Fact]
	public async Task ThemeBrightnessToggle_Click_InDarkMode_SwitchesToLight()
	{
		// Arrange – start in dark mode
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");
		var cut = Render<ThemeBrightnessToggleComponent>();

		// Act
		await cut.InvokeAsync(() => cut.Find($"button#{ToggleButtonId}").Click());

		// Assert – after toggle the title should invite switching back to dark
		var button = cut.Find($"button#{ToggleButtonId}");
		button.GetAttribute("title").Should().Be("Switch to dark mode",
			"clicking in dark mode should activate light mode");
	}
}
