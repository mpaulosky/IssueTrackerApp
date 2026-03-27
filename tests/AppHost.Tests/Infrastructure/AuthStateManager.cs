// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuthStateManager.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using Microsoft.Playwright;

namespace AppHost.Tests.Infrastructure;

/// <summary>
/// Thread-safe singleton-style manager for Auth0 login state.
/// Performs the Auth0 login once and caches the storage state for subsequent tests.
/// </summary>
public static class AuthStateManager
{
	private static readonly SemaphoreSlim _stateLock = new(1, 1);
	private static string? _stateFilePath;

	/// <summary>
	/// Returns the path to the cached Playwright storage state file after performing Auth0 login.
	/// Returns <see langword="null"/> if <c>PLAYWRIGHT_TEST_EMAIL</c> or
	/// <c>PLAYWRIGHT_TEST_PASSWORD</c> environment variables are not set.
	/// </summary>
	/// <param name="page">A browser page used to perform the login flow.</param>
	/// <param name="baseUrl">The base URL of the application (e.g. https://localhost:5001).</param>
	/// <returns>Path to the JSON storage-state file, or <see langword="null"/> if credentials are absent.</returns>
	public static async Task<string?> GetStorageStatePathAsync(IPage page, string baseUrl)
	{
		var email = Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_EMAIL");
		var password = Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_PASSWORD");

		if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
		{
			return null;
		}

		await _stateLock.WaitAsync();
		try
		{
			if (_stateFilePath is not null)
			{
				return _stateFilePath;
			}

			var loginUrl = $"{baseUrl.TrimEnd('/')}/account/login?returnUrl=/";
			await page.GotoAsync(loginUrl);
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			await page.FillAsync("input[name=\"username\"]", email);
			await page.FillAsync("input[name=\"password\"]", password);
			await page.ClickAsync("button[type=\"submit\"]");

			await page.WaitForURLAsync(
				url => url.StartsWith(baseUrl),
				new PageWaitForURLOptions { Timeout = 30_000 });

			var statePath = Path.Combine(Path.GetTempPath(), "issuetracker-playwright-auth.json");

			await page.Context.StorageStateAsync(new BrowserContextStorageStateOptions
			{
				Path = statePath
			});

			_stateFilePath = statePath;
			return _stateFilePath;
		}
		finally
		{
			_stateLock.Release();
		}
	}
}
