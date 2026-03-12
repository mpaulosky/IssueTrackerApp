// Copyright (c) 2024-2025. IssueTracker Project.
// Theme management for dark mode and color schemes
// SPDX-License-Identifier: MIT

/**
 * Theme Manager - Handles dark mode and color scheme preferences
 * Stores preferences in localStorage and applies them to the DOM
 */
window.themeManager = {
	/**
	 * Gets the current theme mode from localStorage
	 * @returns {'light' | 'dark' | 'system'} The current theme mode
	 */
	getThemeMode: function () {
		return localStorage.getItem('theme-mode') || 'system';
	},

	/**
	 * Sets the theme mode and applies it
	 * @param {'light' | 'dark' | 'system'} mode - The theme mode to set
	 */
	setThemeMode: function (mode) {
		localStorage.setItem('theme-mode', mode);
		this.applyTheme();
	},

	/**
	 * Gets the current color scheme from localStorage
	 * @returns {'blue' | 'red' | 'green' | 'yellow'} The current color scheme
	 */
	getColorScheme: function () {
		return localStorage.getItem('theme-color') || 'blue';
	},

	/**
	 * Sets the color scheme and applies it
	 * @param {'blue' | 'red' | 'green' | 'yellow'} scheme - The color scheme to set
	 */
	setColorScheme: function (scheme) {
		localStorage.setItem('theme-color', scheme);
		this.applyTheme();
	},

	/**
	 * Determines if dark mode should be active based on mode and system preference
	 * @returns {boolean} True if dark mode should be active
	 */
	shouldUseDarkMode: function () {
		const mode = this.getThemeMode();
		if (mode === 'dark') return true;
		if (mode === 'light') return false;
		// System mode - check system preference
		return window.matchMedia('(prefers-color-scheme: dark)').matches;
	},

	/**
	 * Applies the current theme settings to the DOM
	 */
	applyTheme: function () {
		const html = document.documentElement;
		const isDark = this.shouldUseDarkMode();
		const colorScheme = this.getColorScheme();

		// Apply dark mode class
		if (isDark) {
			html.classList.add('dark');
		} else {
			html.classList.remove('dark');
		}

		// Apply color scheme data attribute
		html.setAttribute('data-theme', colorScheme);
	},

	/**
	 * Watches for system preference changes and notifies Blazor
	 * @param {object} dotNetHelper - The DotNet object reference for callbacks
	 */
	watchSystemPreference: function (dotNetHelper) {
		const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

		const handler = (e) => {
			// Only apply if we're in system mode
			if (this.getThemeMode() === 'system') {
				this.applyTheme();
				if (dotNetHelper) {
					dotNetHelper.invokeMethodAsync('OnSystemPreferenceChanged', e.matches);
				}
			}
		};

		// Modern browsers
		if (mediaQuery.addEventListener) {
			mediaQuery.addEventListener('change', handler);
		} else {
			// Fallback for older browsers
			mediaQuery.addListener(handler);
		}
	},

	/**
	 * Initializes the theme on page load
	 * Should be called as early as possible to prevent flash
	 */
	initialize: function () {
		this.applyTheme();
	}
};

// Initialize theme immediately to prevent flash of unstyled content
window.themeManager.initialize();
