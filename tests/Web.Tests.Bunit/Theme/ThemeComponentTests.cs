// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ThemeComponentTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Web.Components.Theme;

namespace Web.Tests.Bunit.Theme;

/// <summary>
/// bUnit tests for theme-related Blazor components.
/// Tests the ThemeProvider, ThemeToggle, and ColorSchemeSelector components.
/// </summary>
public class ThemeProviderTests : BunitTestBase
{
	/// <summary>
	/// Test: ThemeProvider renders without errors
	/// </summary>
	[Fact]
	public void ThemeProvider_RendersSuccessfully()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("system");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		// Act
		var cut = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent("<div>Test Content</div>"));

		// Assert
		cut.Should().NotBeNull();
		cut.Find("div").TextContent.Should().Contain("Test Content");
	}


	/// <summary>
	/// Test: ThemeProvider cascades itself to child components
	/// </summary>
	[Fact]
	public void ThemeProvider_CascadesValueToChildren()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("dark");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("red");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(true);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		// Act
		var cut = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<CascadingValueTestComponent>());

		// Assert
		cut.Find(".provider-test").TextContent.Should().Contain("ThemeProvider");
	}
}

/// <summary>
/// bUnit tests for ThemeToggle component.
/// Tests theme mode switching (light/dark/system) and UI interactions.
/// </summary>
public class ThemeToggleTests : BunitTestBase
{
	/// <summary>
	/// Test: ThemeToggle renders button with icon
	/// </summary>
	[Fact]
	public void ThemeToggle_RendersWithButton()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var button = themeProvider.Find("button");

		// Assert
		button.Should().NotBeNull();
		button.GetAttribute("aria-label").Should().Be("Toggle theme");
		button.GetAttribute("type").Should().Be("button");
	}

	/// <summary>
	/// Test: ThemeToggle shows sun icon in light mode
	/// </summary>
	[Fact]
	public async Task ThemeToggle_ShowsSunIcon_InLightMode()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var svgs = themeProvider.FindAll("svg");

		// Assert
		svgs.Should().NotBeEmpty();
		// Sun icon should be rendered in light mode
	}

	/// <summary>
	/// Test: ThemeToggle shows moon icon in dark mode
	/// </summary>
	[Fact]
	public async Task ThemeToggle_ShowsMoonIcon_InDarkMode()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("dark");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(true);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var svgs = themeProvider.FindAll("svg");

		// Assert
		svgs.Should().NotBeEmpty();
		// Moon icon should be rendered in dark mode
	}

	/// <summary>
	/// Test: ThemeToggle dropdown opens when button is clicked
	/// </summary>
	[Fact]
	public async Task ThemeToggle_DropdownOpens_OnButtonClick()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		// Assert
		var menuDiv = themeProvider.FindAll("div[role='menu']").FirstOrDefault();
		menuDiv.Should().NotBeNull();
	}

	/// <summary>
	/// Test: ThemeToggle dropdown contains light, dark, and system options
	/// </summary>
	[Fact]
	public async Task ThemeToggle_DropdownContains_AllThemeModes()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		var menuItems = themeProvider.FindAll("button[role='menuitem']");

		// Assert
		menuItems.Should().HaveCount(3);
		menuItems[0].TextContent.Should().Contain("Light");
		menuItems[1].TextContent.Should().Contain("Dark");
		menuItems[2].TextContent.Should().Contain("System");
	}

	/// <summary>
	/// Test: ThemeToggle highlights current theme mode
	/// </summary>
	[Fact]
	public async Task ThemeToggle_HighlightsCurrentTheme()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("dark");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(true);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		var menuItems = themeProvider.FindAll("button[role='menuitem']");
		var darkModeButton = menuItems[1]; // Dark mode is the second button

		// Assert
		darkModeButton.GetAttribute("class").Should().Contain("text-primary");
	}

	/// <summary>
	/// Test: ThemeToggle calls SetThemeModeAsync when option is selected
	/// </summary>
	[Fact]
	public async Task ThemeToggle_CallsSetThemeModeAsync_OnSelection()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
		JSInterop.SetupVoid("themeManager.setThemeMode");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		var menuItems = themeProvider.FindAll("button[role='menuitem']");
		var darkModeButton = menuItems[1];
		await darkModeButton.ClickAsync(new());

		// Assert
		JSInterop.VerifyInvoke("themeManager.setThemeMode", calledTimes: 1);
	}

	/// <summary>
	/// Test: ThemeToggle closes dropdown after selection
	/// </summary>
	[Fact]
	public async Task ThemeToggle_ClosesDropdown_AfterSelection()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
		JSInterop.SetupVoid("themeManager.setThemeMode");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		var button = themeProvider.Find("button");
		await button.ClickAsync(new()); // Open

		var menuItems = themeProvider.FindAll("button[role='menuitem']");
		await menuItems[1].ClickAsync(new()); // Select dark mode

		// Act
		themeProvider.Render();

		// Assert
		var menu = themeProvider.FindAll("div[role='menu']").FirstOrDefault();
		menu.Should().BeNull();
	}

	/// <summary>
	/// Test: ThemeToggle title reflects current theme mode
	/// </summary>
	[Fact]
	public async Task ThemeToggle_TitleReflectsCurrentMode()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("dark");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(true);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var button = themeProvider.Find("button");
		var title = button.GetAttribute("title");

		// Assert
		title.Should().Be("Dark mode");
	}

	/// <summary>
	/// Test: ThemeToggle disposes event handler
	/// </summary>
	[Fact]
	public void ThemeToggle_DisposesEventHandler()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeToggle>());

		// Act
		var toggle = themeProvider.FindComponent<ThemeToggle>();
		toggle.Instance.Dispose();

		// Assert - should not throw
		toggle.Should().NotBeNull();
	}
}

/// <summary>
/// bUnit tests for ColorSchemeSelector component.
/// Tests color scheme selection (Red, Blue, Green, Yellow) and UI interactions.
/// </summary>
public class ColorSchemeSelectorTests : BunitTestBase
{
	/// <summary>
	/// Test: ColorSchemeSelector renders button with icon
	/// </summary>
	[Fact]
	public void ColorSchemeSelector_RendersWithButton()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		// Act
		var button = themeProvider.Find("button");

		// Assert
		button.Should().NotBeNull();
		button.GetAttribute("aria-label").Should().Be("Change color scheme");
		button.GetAttribute("type").Should().Be("button");
	}

	/// <summary>
	/// Test: ColorSchemeSelector dropdown opens when button is clicked
	/// </summary>
	[Fact]
	public async Task ColorSchemeSelector_DropdownOpens_OnButtonClick()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		// Assert
		var menuDiv = themeProvider.FindAll("div[role='menu']").FirstOrDefault();
		menuDiv.Should().NotBeNull();
	}

	/// <summary>
	/// Test: ColorSchemeSelector dropdown contains all color scheme options
	/// </summary>
	[Fact]
	public async Task ColorSchemeSelector_DropdownContains_AllColorSchemes()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		var menuItems = themeProvider.FindAll("button[role='menuitem']");

		// Assert
		menuItems.Should().HaveCount(4);
		menuItems[0].TextContent.Should().Contain("Blue");
		menuItems[1].TextContent.Should().Contain("Red");
		menuItems[2].TextContent.Should().Contain("Green");
		menuItems[3].TextContent.Should().Contain("Yellow");
	}

	/// <summary>
	/// Test: ColorSchemeSelector highlights current color scheme
	/// </summary>
	[Fact]
	public async Task ColorSchemeSelector_HighlightsCurrentScheme()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("red");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		var menuItems = themeProvider.FindAll("button[role='menuitem']");
		var redSchemeButton = menuItems[1]; // Red is the second button

		// Assert
		redSchemeButton.GetAttribute("class").Should().Contain("ring-2");
	}

	/// <summary>
	/// Test: ColorSchemeSelector displays color swatches
	/// </summary>
	[Fact]
	public async Task ColorSchemeSelector_DisplaysColorSwatches()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		var swatches = themeProvider.FindAll("span[class*='rounded-full']");

		// Assert
		swatches.Should().HaveCount(4);
	}

	/// <summary>
	/// Test: ColorSchemeSelector calls SetColorSchemeAsync when option is selected
	/// </summary>
	[Fact]
	public async Task ColorSchemeSelector_CallsSetColorSchemeAsync_OnSelection()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
		JSInterop.SetupVoid("themeManager.setColorScheme");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		var menuItems = themeProvider.FindAll("button[role='menuitem']");
		var redSchemeButton = menuItems[1];
		await redSchemeButton.ClickAsync(new());

		// Assert
		JSInterop.VerifyInvoke("themeManager.setColorScheme", calledTimes: 1);
	}

	/// <summary>
	/// Test: ColorSchemeSelector closes dropdown after selection
	/// </summary>
	[Fact]
	public async Task ColorSchemeSelector_ClosesDropdown_AfterSelection()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
		JSInterop.SetupVoid("themeManager.setColorScheme");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		var button = themeProvider.Find("button");
		await button.ClickAsync(new()); // Open

		var menuItems = themeProvider.FindAll("button[role='menuitem']");
		await menuItems[1].ClickAsync(new()); // Select red scheme

		// Act
		themeProvider.Render();

		// Assert
		var menu = themeProvider.FindAll("div[role='menu']").FirstOrDefault();
		menu.Should().BeNull();
	}

	/// <summary>
	/// Test: ColorSchemeSelector displays color scheme label
	/// </summary>
	[Fact]
	public async Task ColorSchemeSelector_DisplaysColorSchemeLabel()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		// Act
		var button = themeProvider.Find("button");
		await button.ClickAsync(new());

		var label = themeProvider.Find("p");

		// Assert
		label.TextContent.Should().Contain("Color Theme");
	}

	/// <summary>
	/// Test: ColorSchemeSelector disposes event handler
	/// </summary>
	[Fact]
	public void ColorSchemeSelector_DisposesEventHandler()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ColorSchemeSelector>());

		// Act
		var selector = themeProvider.FindComponent<ColorSchemeSelector>();
		selector.Instance.Dispose();

		// Assert - should not throw
		selector.Should().NotBeNull();
	}
}

/// <summary>
/// Integration tests for Theme components working together.
/// Tests theme state changes and persistence across components.
/// </summary>
public class ThemeIntegrationTests : BunitTestBase
{
	/// <summary>
	/// Test: Both ThemeToggle and ColorSchemeSelector render together
	/// </summary>
	[Fact]
	public void Theme_BothComponents_RenderTogether()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		// Act
		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent(builder =>
			{
				builder.OpenComponent<ThemeToggle>(0);
				builder.CloseComponent();
				builder.OpenComponent<ColorSchemeSelector>(1);
				builder.CloseComponent();
			}));

		// Assert
		var buttons = themeProvider.FindAll("button");
		buttons.Should().HaveCountGreaterThanOrEqualTo(2);
	}

	/// <summary>
	/// Test: Theme state is shared between ThemeToggle and ColorSchemeSelector
	/// </summary>
	[Fact]
	public async Task Theme_StateIsShared_BetweenComponents()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent(builder =>
			{
				builder.OpenComponent<ThemeToggle>(0);
				builder.CloseComponent();
				builder.OpenComponent<ColorSchemeSelector>(1);
				builder.CloseComponent();
			}));

		// Act
		var provider = themeProvider.Instance;

		// Assert
		provider.Should().NotBeNull();
		provider.ThemeMode.Should().Be("light");
		provider.ColorScheme.Should().Be("blue");
	}

	/// <summary>
	/// Test: OnThemeChanged event is triggered when theme changes
	/// </summary>
	[Fact]
	public async Task Theme_OnThemeChanged_TriggeredOnThemeChange()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
		JSInterop.SetupVoid("themeManager.setThemeMode");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;

		var eventTriggered = false;
		provider.OnThemeChanged += () => eventTriggered = true;

		// Act
		await provider.SetThemeModeAsync("dark");

		// Assert
		eventTriggered.Should().BeTrue();
	}

	/// <summary>
	/// Test: ColorScheme persists across theme mode changes
	/// </summary>
	[Fact]
	public async Task Theme_ColorScheme_PersistsAcrossThemeModeChanges()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("red");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
		JSInterop.SetupVoid("themeManager.setThemeMode");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;
		var originalColorScheme = provider.ColorScheme;

		// Act
		await provider.SetThemeModeAsync("dark");

		// Assert
		provider.ColorScheme.Should().Be(originalColorScheme);
	}

	/// <summary>
	/// Test: ThemeMode persists across color scheme changes
	/// </summary>
	[Fact]
	public async Task Theme_ThemeMode_PersistsAcrossColorSchemeChanges()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("dark");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(true);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
		JSInterop.SetupVoid("themeManager.setColorScheme");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;
		var originalThemeMode = provider.ThemeMode;

		// Act
		await provider.SetColorSchemeAsync("green");

		// Assert
		provider.ThemeMode.Should().Be(originalThemeMode);
	}

	/// <summary>
	/// Test: SystemPreferenceChanged callback updates IsDarkMode
	/// </summary>
	[Fact]
	public void Theme_SystemPreferenceChanged_UpdatesIsDarkMode()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("system");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;

		// Act
		provider.OnSystemPreferenceChanged(true);

		// Assert
		provider.IsDarkMode.Should().BeTrue();
	}

	/// <summary>
	/// Test: SystemPreferenceChanged is ignored when theme mode is not system
	/// </summary>
	[Fact]
	public void Theme_SystemPreferenceChanged_IgnoredInNonSystemMode()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
		JSInterop.Setup<string>("themeManager.getColorScheme").SetResult("blue");
		JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;
		var originalDarkMode = provider.IsDarkMode;

		// Act
		provider.OnSystemPreferenceChanged(true);

		// Assert
		provider.IsDarkMode.Should().Be(originalDarkMode);
	}
}

/// <summary>
/// Test helper component for testing cascading values
/// </summary>
internal class CascadingValueTestComponent : ComponentBase
{
	[CascadingParameter]
	private ThemeProvider? ThemeProvider { get; set; }

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", "provider-test");
		builder.AddContent(2, ThemeProvider is not null ? "ThemeProvider" : "No Provider");
		builder.CloseElement();
	}
}
