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
/// Tests the ThemeProvider and ThemeSelector components.
/// </summary>
public class ThemeProviderTests : BunitTestBase
{
	private void SetupJsInterop(string color = "blue", string brightness = "light")
	{
		JSInterop.Setup<string>("themeManager.getColor").SetResult(color);
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult(brightness);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
	}

	/// <summary>
	/// Test: ThemeProvider renders without errors
	/// </summary>
	[Fact]
	public void ThemeProvider_RendersSuccessfully()
	{
		// Arrange
		SetupJsInterop();

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
		SetupJsInterop("red", "dark");

		// Act
		var cut = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<CascadingValueTestComponent>());

		// Assert
		cut.Find(".provider-test").TextContent.Should().Contain("ThemeProvider");
	}
}

/// <summary>
/// bUnit tests for ThemeSelector component.
/// Tests combined color and brightness selection with pill buttons.
/// </summary>
public class ThemeSelectorTests : BunitTestBase
{
	private void SetupJsInterop(string color = "blue", string brightness = "light")
	{
		JSInterop.Setup<string>("themeManager.getColor").SetResult(color);
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult(brightness);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
	}

	/// <summary>
	/// Test: ThemeSelector renders section with heading
	/// </summary>
	[Fact]
	public void ThemeSelector_RendersWithHeading()
	{
		// Arrange
		SetupJsInterop();

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var heading = themeProvider.Find("h2");

		// Assert
		heading.Should().NotBeNull();
		heading.TextContent.Should().Contain("Choose Your Theme");
	}

	/// <summary>
	/// Test: ThemeSelector displays all four color buttons
	/// </summary>
	[Fact]
	public void ThemeSelector_DisplaysAllColorButtons()
	{
		// Arrange
		SetupJsInterop();

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var buttons = themeProvider.FindAll("button");
		var buttonTexts = buttons.Select(b => b.TextContent.Trim()).ToList();

		// Assert
		buttonTexts.Should().Contain("Red");
		buttonTexts.Should().Contain("Blue");
		buttonTexts.Should().Contain("Green");
		buttonTexts.Should().Contain("Yellow");
	}

	/// <summary>
	/// Test: ThemeSelector displays brightness buttons
	/// </summary>
	[Fact]
	public void ThemeSelector_DisplaysBrightnessButtons()
	{
		// Arrange
		SetupJsInterop();

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var buttons = themeProvider.FindAll("button");
		var buttonTexts = buttons.Select(b => b.TextContent.Trim()).ToList();

		// Assert
		buttonTexts.Should().Contain("Light(Pastel)");
		buttonTexts.Should().Contain("Dark(Rich)");
	}

	/// <summary>
	/// Test: ThemeSelector shows current theme label
	/// </summary>
	[Fact]
	public void ThemeSelector_ShowsCurrentThemeLabel()
	{
		// Arrange
		SetupJsInterop("blue", "light");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var section = themeProvider.Find("section");

		// Assert
		section.TextContent.Should().Contain("Current:");
		section.TextContent.Should().Contain("BLUE Light");
	}

	/// <summary>
	/// Test: ThemeSelector highlights active color button
	/// </summary>
	[Fact]
	public void ThemeSelector_HighlightsActiveColorButton()
	{
		// Arrange
		SetupJsInterop("red", "light");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var buttons = themeProvider.FindAll("button");
		var redButton = buttons.FirstOrDefault(b => b.TextContent.Trim() == "Red");

		// Assert
		redButton.Should().NotBeNull();
		redButton!.GetAttribute("class").Should().Contain("ring-2");
	}

	/// <summary>
	/// Test: ThemeSelector highlights active brightness button
	/// </summary>
	[Fact]
	public void ThemeSelector_HighlightsActiveBrightnessButton()
	{
		// Arrange
		SetupJsInterop("blue", "dark");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var buttons = themeProvider.FindAll("button");
		var darkButton = buttons.FirstOrDefault(b => b.TextContent.Trim() == "Dark(Rich)");

		// Assert
		darkButton.Should().NotBeNull();
		darkButton!.GetAttribute("class").Should().Contain("ring-2");
	}

	/// <summary>
	/// Test: ThemeSelector calls setColor when color button is clicked
	/// </summary>
	[Fact]
	public async Task ThemeSelector_CallsSetColor_OnColorButtonClick()
	{
		// Arrange
		SetupJsInterop();
		JSInterop.SetupVoid("themeManager.setColor");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var buttons = themeProvider.FindAll("button");
		var redButton = buttons.First(b => b.TextContent.Trim() == "Red");
		await redButton.ClickAsync(new());

		// Assert
		JSInterop.VerifyInvoke("themeManager.setColor", calledTimes: 1);
	}

	/// <summary>
	/// Test: ThemeSelector calls setBrightness when brightness button is clicked
	/// </summary>
	[Fact]
	public async Task ThemeSelector_CallsSetBrightness_OnBrightnessButtonClick()
	{
		// Arrange
		SetupJsInterop();
		JSInterop.SetupVoid("themeManager.setBrightness");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var buttons = themeProvider.FindAll("button");
		var darkButton = buttons.First(b => b.TextContent.Trim() == "Dark(Rich)");
		await darkButton.ClickAsync(new());

		// Assert
		JSInterop.VerifyInvoke("themeManager.setBrightness", calledTimes: 1);
	}

	/// <summary>
	/// Test: ThemeSelector shows color dot indicators
	/// </summary>
	[Fact]
	public void ThemeSelector_ShowsColorDotIndicators()
	{
		// Arrange
		SetupJsInterop();

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var dots = themeProvider.FindAll("span[class*='rounded-full']");

		// Assert
		dots.Should().HaveCount(4);
	}

	/// <summary>
	/// Test: ThemeSelector disposes event handler
	/// </summary>
	[Fact]
	public void ThemeSelector_DisposesEventHandler()
	{
		// Arrange
		SetupJsInterop();

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var selector = themeProvider.FindComponent<ThemeSelector>();
		selector.Instance.Dispose();

		// Assert - should not throw
		selector.Should().NotBeNull();
	}
}

/// <summary>
/// Integration tests for Theme components working together.
/// Tests theme state changes and persistence.
/// </summary>
public class ThemeIntegrationTests : BunitTestBase
{
	private void SetupJsInterop(string color = "blue", string brightness = "light")
	{
		JSInterop.Setup<string>("themeManager.getColor").SetResult(color);
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult(brightness);
		JSInterop.SetupVoid("themeManager.watchSystemPreference");
	}

	/// <summary>
	/// Test: ThemeSelector renders within ThemeProvider
	/// </summary>
	[Fact]
	public void Theme_ThemeSelector_RendersWithinProvider()
	{
		// Arrange
		SetupJsInterop();

		// Act
		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Assert
		var buttons = themeProvider.FindAll("button");
		buttons.Should().HaveCountGreaterThanOrEqualTo(6); // 4 colors + 2 brightness
	}

	/// <summary>
	/// Test: Theme state is accessible via ThemeProvider
	/// </summary>
	[Fact]
	public void Theme_StateIsAccessible_ViaProvider()
	{
		// Arrange
		SetupJsInterop("red", "dark");

		var themeProvider = Render<ThemeProvider>(parameters =>
			parameters.AddChildContent<ThemeSelector>());

		// Act
		var provider = themeProvider.Instance;

		// Assert
		provider.Should().NotBeNull();
		provider.Color.Should().Be("red");
		provider.Brightness.Should().Be("dark");
		provider.IsDarkMode.Should().BeTrue();
	}

	/// <summary>
	/// Test: OnThemeChanged event is triggered when color changes
	/// </summary>
	[Fact]
	public async Task Theme_OnThemeChanged_TriggeredOnColorChange()
	{
		// Arrange
		SetupJsInterop();
		JSInterop.SetupVoid("themeManager.setColor");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;

		var eventTriggered = false;
		provider.OnThemeChanged += () => eventTriggered = true;

		// Act
		await themeProvider.InvokeAsync(async () => await provider.SetColorAsync("red"));

		// Assert
		eventTriggered.Should().BeTrue();
	}

	/// <summary>
	/// Test: OnThemeChanged event is triggered when brightness changes
	/// </summary>
	[Fact]
	public async Task Theme_OnThemeChanged_TriggeredOnBrightnessChange()
	{
		// Arrange
		SetupJsInterop();
		JSInterop.SetupVoid("themeManager.setBrightness");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;

		var eventTriggered = false;
		provider.OnThemeChanged += () => eventTriggered = true;

		// Act
		await themeProvider.InvokeAsync(async () => await provider.SetBrightnessAsync("dark"));

		// Assert
		eventTriggered.Should().BeTrue();
	}

	/// <summary>
	/// Test: Color persists across brightness changes
	/// </summary>
	[Fact]
	public async Task Theme_Color_PersistsAcrossBrightnessChanges()
	{
		// Arrange
		SetupJsInterop("red", "light");
		JSInterop.SetupVoid("themeManager.setBrightness");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;
		var originalColor = provider.Color;

		// Act
		await themeProvider.InvokeAsync(async () => await provider.SetBrightnessAsync("dark"));

		// Assert
		provider.Color.Should().Be(originalColor);
	}

	/// <summary>
	/// Test: Brightness persists across color changes
	/// </summary>
	[Fact]
	public async Task Theme_Brightness_PersistsAcrossColorChanges()
	{
		// Arrange
		SetupJsInterop("blue", "dark");
		JSInterop.SetupVoid("themeManager.setColor");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;
		var originalBrightness = provider.Brightness;

		// Act
		await themeProvider.InvokeAsync(async () => await provider.SetColorAsync("green"));

		// Assert
		provider.Brightness.Should().Be(originalBrightness);
	}

	/// <summary>
	/// Test: SystemPreferenceChanged callback triggers event
	/// </summary>
	[Fact]
	public void Theme_SystemPreferenceChanged_TriggersEvent()
	{
		// Arrange
		SetupJsInterop();

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;

		var eventTriggered = false;
		provider.OnThemeChanged += () => eventTriggered = true;

		// Act
		themeProvider.InvokeAsync(() => provider.OnSystemPreferenceChanged(true));

		// Assert
		eventTriggered.Should().BeTrue();
	}

	/// <summary>
	/// Test: IsDarkMode reflects brightness state
	/// </summary>
	[Fact]
	public void Theme_IsDarkMode_ReflectsBrightness()
	{
		// Arrange - light brightness
		SetupJsInterop("blue", "light");

		var themeProvider = Render<ThemeProvider>();
		var provider = themeProvider.Instance;

		// Assert
		provider.IsDarkMode.Should().BeFalse();
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
