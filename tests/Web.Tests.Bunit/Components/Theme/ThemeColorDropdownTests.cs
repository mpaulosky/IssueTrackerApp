// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ThemeColorDropdownTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Theme;

namespace Web.Tests.Bunit.Components.Theme;

/// <summary>
///   bUnit tests for the ThemeColorDropdownComponent.
///   The component renders a trigger button that, when clicked, exposes
///   four color-swatch buttons (Blue, Red, Green, Yellow).
///   It reads the current color from themeManager.getColor on first render
///   and writes back via themeManager.setColor when a swatch is selected.
///   It does NOT use a CascadingParameter — it manages its own JSInterop.
/// </summary>
public class ThemeColorDropdownTests : BunitTestBase
{
	private const string TriggerAriaLabel = "Choose color theme";

	// -----------------------------------------------------------------------
	// Rendering
	// -----------------------------------------------------------------------

	[Fact]
	public void ThemeColorDropdown_RendersWithoutCrashing()
	{
		// Act
		var cut = Render<ThemeColorDropdownComponent>();

		// Assert – trigger button is always present
		cut.Find($"button[aria-label='{TriggerAriaLabel}']").Should().NotBeNull(
			"the color picker trigger button must always be rendered");
	}

	[Fact]
	public void ThemeColorDropdown_TriggerButton_HasCorrectAriaAttributes()
	{
		// Act
		var cut = Render<ThemeColorDropdownComponent>();

		// Assert
		var button = cut.Find($"button[aria-label='{TriggerAriaLabel}']");
		button.GetAttribute("aria-haspopup").Should().Be("true",
			"the button must signal to assistive technology that it opens a popup");
		// Blazor omits aria-expanded when the bool value is false (attribute absent == collapsed)
		button.HasAttribute("aria-expanded").Should().BeFalse(
			"aria-expanded should be absent (false) on initial render");
	}

	// -----------------------------------------------------------------------
	// Dropdown visibility
	// -----------------------------------------------------------------------

	[Fact]
	public void ThemeColorDropdown_ColorSwatches_AreHidden_BeforeClick()
	{
		// Act
		var cut = Render<ThemeColorDropdownComponent>();

		// Assert – swatch buttons (each has an aria-label containing "color theme") should not be in DOM
		var swatches = cut.FindAll("button[aria-label*='color theme']");
		swatches.Should().NotContain(
			b => b.GetAttribute("aria-label") != TriggerAriaLabel,
			"color swatches must be hidden until the trigger is clicked");
	}

	[Fact]
	public async Task ThemeColorDropdown_AfterClick_ShowsAllFourColorSwatches()
	{
		// Arrange
		var cut = Render<ThemeColorDropdownComponent>();
		var trigger = cut.Find($"button[aria-label='{TriggerAriaLabel}']");

		// Act
		await cut.InvokeAsync(() => trigger.Click());

		// Assert – four swatch buttons are now visible
		cut.Find("button[aria-label='Blue color theme']").Should().NotBeNull();
		cut.Find("button[aria-label='Red color theme']").Should().NotBeNull();
		cut.Find("button[aria-label='Green color theme']").Should().NotBeNull();
		cut.Find("button[aria-label='Yellow color theme']").Should().NotBeNull();
	}

	[Fact]
	public async Task ThemeColorDropdown_AfterClick_TriggerIsMarkedExpanded()
	{
		// Arrange
		var cut = Render<ThemeColorDropdownComponent>();
		var trigger = cut.Find($"button[aria-label='{TriggerAriaLabel}']");

		// Act
		await cut.InvokeAsync(() => trigger.Click());

		// Assert
		// Blazor sets aria-expanded attribute (present = expanded) when the bool is true;
		// when false, the attribute is absent.  Check presence, not the string value.
		cut.Find($"button[aria-label='{TriggerAriaLabel}']")
		   .HasAttribute("aria-expanded").Should().BeTrue(
			"aria-expanded must be present after the dropdown opens");
	}

	// -----------------------------------------------------------------------
	// Color selection
	// -----------------------------------------------------------------------

	[Fact]
	public async Task ThemeColorDropdown_SelectingColor_ClosesDropdown()
	{
		// Arrange – open the dropdown
		var cut = Render<ThemeColorDropdownComponent>();
		await cut.InvokeAsync(() => cut.Find($"button[aria-label='{TriggerAriaLabel}']").Click());

		// Act – pick Red
		await cut.InvokeAsync(() => cut.Find("button[aria-label='Red color theme']").Click());

		// Assert – dropdown collapses after a selection; Blazor removes aria-expanded when false
		cut.Find($"button[aria-label='{TriggerAriaLabel}']")
		   .HasAttribute("aria-expanded").Should().BeFalse(
			"aria-expanded must be absent (collapsed) after a color is chosen");
	}

	[Fact]
	public async Task ThemeColorDropdown_SelectingColor_CallsJsSetColor()
	{
		// Arrange
		JSInterop.SetupVoid("themeManager.setColor", _ => true);
		var cut = Render<ThemeColorDropdownComponent>();
		await cut.InvokeAsync(() => cut.Find($"button[aria-label='{TriggerAriaLabel}']").Click());

		// Act
		await cut.InvokeAsync(() => cut.Find("button[aria-label='Blue color theme']").Click());

		// Assert – exactly one call with the correct lowercase argument
		var setColorCalls = JSInterop.Invocations
			.Where(x => x.Identifier == "themeManager.setColor")
			.ToList();
		setColorCalls.Should().HaveCount(1,
			"selecting a color must persist the choice via exactly one themeManager.setColor call");
		setColorCalls[0].Arguments[0].Should().Be("blue",
			"clicking the Blue swatch must call setColor with lowercase \"blue\"");
	}
}
