// Copyright (c) 2024-2025. IssueTracker Project.
// Theme Provider component code-behind for JS interop
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Web.Components.Theme;

/// <summary>
/// Provides theme management capabilities including dark mode and color scheme switching.
/// Uses JavaScript interop to persist preferences in localStorage.
/// </summary>
public partial class ThemeProvider : IAsyncDisposable
{
	[Inject]
	private IJSRuntime JsRuntime { get; set; } = default!;

	private DotNetObjectReference<ThemeProvider>? _dotNetRef;
	private string _themeMode = "system";
	private string _colorScheme = "blue";
	private bool _isDarkMode;
	private bool _isInitialized;

	/// <summary>
	/// Gets the current theme mode (light, dark, or system)
	/// </summary>
	public string ThemeMode => _themeMode;

	/// <summary>
	/// Gets the current color scheme (blue, red, green, or yellow)
	/// </summary>
	public string ColorScheme => _colorScheme;

	/// <summary>
	/// Gets whether dark mode is currently active
	/// </summary>
	public bool IsDarkMode => _isDarkMode;

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
			_themeMode = await JsRuntime.InvokeAsync<string>("themeManager.getThemeMode");
			_colorScheme = await JsRuntime.InvokeAsync<string>("themeManager.getColorScheme");
			_isDarkMode = await JsRuntime.InvokeAsync<bool>("themeManager.shouldUseDarkMode");

			// Watch for system preference changes
			await JsRuntime.InvokeVoidAsync("themeManager.watchSystemPreference", _dotNetRef);

			_isInitialized = true;
			OnThemeChanged?.Invoke();
			StateHasChanged();
		}
		catch (JSException)
		{
			// JS interop not available (prerendering)
		}
	}

	/// <summary>
	/// Sets the theme mode and persists it
	/// </summary>
	/// <param name="mode">The theme mode: "light", "dark", or "system"</param>
	public async Task SetThemeModeAsync(string mode)
	{
		if (!_isInitialized)
		{
			return;
		}

		_themeMode = mode;
		await JsRuntime.InvokeVoidAsync("themeManager.setThemeMode", mode);
		_isDarkMode = await JsRuntime.InvokeAsync<bool>("themeManager.shouldUseDarkMode");
		OnThemeChanged?.Invoke();
		StateHasChanged();
	}

	/// <summary>
	/// Sets the color scheme and persists it
	/// </summary>
	/// <param name="scheme">The color scheme: "blue", "red", "green", or "yellow"</param>
	public async Task SetColorSchemeAsync(string scheme)
	{
		if (!_isInitialized)
		{
			return;
		}

		_colorScheme = scheme;
		await JsRuntime.InvokeVoidAsync("themeManager.setColorScheme", scheme);
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
		if (_themeMode == "system")
		{
			_isDarkMode = isDark;
			OnThemeChanged?.Invoke();
			StateHasChanged();
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_dotNetRef?.Dispose();
		await ValueTask.CompletedTask;
	}
}
