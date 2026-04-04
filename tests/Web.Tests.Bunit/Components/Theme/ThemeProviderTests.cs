// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ThemeProviderTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Theme;

namespace Web.Tests.Bunit.Components.Theme;

/// <summary>
///   bUnit tests for the ThemeProvider component.
///   ThemeProvider wraps its children in a CascadingValue and reads/writes
///   theme state via JavaScript interop (themeManager.*).
///   Tests rely on JSInterop.Mode = JSRuntimeMode.Loose (set in BunitTestBase)
///   so that un-configured JS calls return default values rather than throwing.
/// </summary>
public class ThemeProviderTests : BunitTestBase
{
	// -----------------------------------------------------------------------
	// Rendering
	// -----------------------------------------------------------------------

	[Fact]
	public void ThemeProvider_RendersChildContent()
	{
		// Act
		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span id='inner-child'>Hello Theme</span>"));

		// Assert – the child markup must be present in the rendered output
		cut.Find("span#inner-child").TextContent.Should().Be("Hello Theme");
	}

	// -----------------------------------------------------------------------
	// IsDarkMode property
	// -----------------------------------------------------------------------

	[Fact]
	public void ThemeProvider_IsDarkMode_IsFalse_WhenBrightnessIsDefault()
	{
		// Arrange – Loose mode: InvokeAsync<string> returns null, so
		// _brightness = null and null != "dark".
		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));

		// Assert
		cut.Instance.IsDarkMode.Should().BeFalse(
			"Loose-mode JS returns null which is not equal to \"dark\"");
	}

	[Fact]
	public void ThemeProvider_IsDarkMode_IsFalse_WhenBrightnessIsLight()
	{
		// Arrange – explicit setup so getBrightness returns "light"
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));

		// Assert
		cut.Instance.IsDarkMode.Should().BeFalse(
			"\"light\" brightness means IsDarkMode should be false");
	}

	[Fact]
	public void ThemeProvider_IsDarkMode_IsTrue_WhenBrightnessIsDark()
	{
		// Arrange – specific setup takes priority over Loose fallback
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));

		// Assert
		cut.Instance.IsDarkMode.Should().BeTrue(
			"\"dark\" brightness means dark mode is active");
	}

	// -----------------------------------------------------------------------
	// Property reflection of JS values
	// -----------------------------------------------------------------------

	[Fact]
	public void ThemeProvider_Color_ReflectsValueReturnedByJs()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("red");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));

		// Assert
		cut.Instance.Color.Should().Be("red",
			"Color should mirror the value returned by themeManager.getColor");
	}

	[Fact]
	public void ThemeProvider_Brightness_ReflectsValueReturnedByJs()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("green");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));

		// Assert
		cut.Instance.Brightness.Should().Be("dark",
			"Brightness should mirror the value returned by themeManager.getBrightness");
	}

	// -----------------------------------------------------------------------
	// OnThemeChanged event
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ThemeProvider_OnThemeChanged_FiresWhenSetColorAsyncCalled()
	{
		// Arrange – render so _isInitialized = true (Loose mode allows all JS calls)
		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));
		var provider = cut.Instance;

		var eventFired = false;
		provider.OnThemeChanged += () => eventFired = true;

		// Act – SetColorAsync only does work when _isInitialized = true
		await cut.InvokeAsync(() => provider.SetColorAsync("green"));

		// Assert
		eventFired.Should().BeTrue(
			"SetColorAsync must raise OnThemeChanged so dependant UI can re-render");
	}

	[Fact]
	public async Task ThemeProvider_OnThemeChanged_FiresWhenSetBrightnessAsyncCalled()
	{
		// Arrange
		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));
		var provider = cut.Instance;

		var eventFired = false;
		provider.OnThemeChanged += () => eventFired = true;

		// Act
		await cut.InvokeAsync(() => provider.SetBrightnessAsync("dark"));

		// Assert
		eventFired.Should().BeTrue(
			"SetBrightnessAsync must raise OnThemeChanged so dependant UI can re-render");
	}

	// -----------------------------------------------------------------------
	// Mutation via SetColorAsync / SetBrightnessAsync
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ThemeProvider_SetColorAsync_UpdatesColorProperty()
	{
		// Arrange
		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));
		var provider = cut.Instance;

		// Act
		await cut.InvokeAsync(() => provider.SetColorAsync("yellow"));

		// Assert
		provider.Color.Should().Be("yellow",
			"SetColorAsync must update the Color property so cascaded children read the new value");
	}

	[Fact]
	public async Task ThemeProvider_SetBrightnessAsync_UpdatesBrightnessProperty()
	{
		// Arrange
		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));
		var provider = cut.Instance;

		// Act
		await cut.InvokeAsync(() => provider.SetBrightnessAsync("dark"));

		// Assert – Loose mode returns null for the subsequent getColor call, which is fine;
		// the brightness field was explicitly set before the JS call.
		provider.Brightness.Should().Be("dark",
			"SetBrightnessAsync must update the Brightness property so cascaded children read the new value");
	}

	// -----------------------------------------------------------------------
	// Disposal
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ThemeProvider_DisposeAsync_DoesNotThrow()
	{
		// Arrange
		var cut = Render<ThemeProvider>(p =>
			p.AddChildContent("<span>child</span>"));

		// Act & Assert
		var act = async () => await cut.Instance.DisposeAsync();
		await act.Should().NotThrowAsync("DisposeAsync must be safe to call");
	}
}
