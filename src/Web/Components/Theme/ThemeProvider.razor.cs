// Copyright (c) 2024-2025. IssueTracker Project.
// Theme Provider component code-behind for JS interop
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Web.Components.Theme;

/// <summary>
/// Provides theme management capabilities including color and brightness switching.
/// Uses JavaScript interop to persist preferences in localStorage.
/// </summary>
public partial class ThemeProvider : IAsyncDisposable
{
	[Inject]
	private IJSRuntime JsRuntime { get; set; } = default!;

	private DotNetObjectReference<ThemeProvider>? _dotNetRef;
	private string _color = "blue";
	private string _brightness = "light";
	private bool _isInitialized;

	/// <summary>
	/// Gets the current color (blue, red, green, or yellow)
	/// </summary>
	public string Color => _color;

	/// <summary>
	/// Gets the current brightness (light or dark)
	/// </summary>
	public string Brightness => _brightness;

	/// <summary>
	/// Gets whether dark mode is currently active
	/// </summary>
	public bool IsDarkMode => _brightness == "dark";

	/// <summary>
	/// Event triggered when the theme changes
	/// </summary>
	public event Action? OnThemeChanged;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_dotNetRef = DotNetObjectReference.Create(this);
			await InitializeThemeAsync();
		}
	}

	private async Task InitializeThemeAsync()
	{
		try
		{
			_color = await JsRuntime.InvokeAsync<string>("themeManager.getColor");
			_brightness = await JsRuntime.InvokeAsync<string>("themeManager.getBrightness");

			// Watch for system preference changes
			await JsRuntime.InvokeVoidAsync("themeManager.watchSystemPreference", _dotNetRef);

			_isInitialized = true;
			OnThemeChanged?.Invoke();
			StateHasChanged();

			// Signal to E2E tests that ThemeProvider is interactive.
			// Playwright tests wait on data-theme-ready="true" before clicking theme controls
			// to avoid clicking before SetBrightnessAsync / SetColorAsync are enabled.
			await JsRuntime.InvokeVoidAsync("themeManager.markInitialized");
		}
		catch (JSException)
		{
			// JS interop not available (prerendering)
		}
	}

	/// <summary>
	/// Sets the color and persists it
	/// </summary>
	/// <param name="color">The color: "blue", "red", "green", or "yellow"</param>
	public async Task SetColorAsync(string color)
	{
		if (!_isInitialized)
		{
			return;
		}

		_color = color;
		await JsRuntime.InvokeVoidAsync("themeManager.setColor", color);
		_brightness = await JsRuntime.InvokeAsync<string>("themeManager.getBrightness");
		OnThemeChanged?.Invoke();
		StateHasChanged();
	}

	/// <summary>
	/// Sets the brightness and persists it
	/// </summary>
	/// <param name="brightness">The brightness: "light" or "dark"</param>
	public async Task SetBrightnessAsync(string brightness)
	{
		if (!_isInitialized)
		{
			return;
		}

		_brightness = brightness;
		await JsRuntime.InvokeVoidAsync("themeManager.setBrightness", brightness);
		_color = await JsRuntime.InvokeAsync<string>("themeManager.getColor");
		OnThemeChanged?.Invoke();
		StateHasChanged();
	}

	/// <summary>
	/// Called from JavaScript when system preference changes
	/// </summary>
	/// <param name="isDark">Whether the system now prefers dark mode</param>
	[JSInvokable]
	public void OnSystemPreferenceChanged(bool isDark)
	{
		// System preference callback - could auto-switch if desired
		OnThemeChanged?.Invoke();
		StateHasChanged();
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_dotNetRef?.Dispose();
		await ValueTask.CompletedTask;
	}
}
