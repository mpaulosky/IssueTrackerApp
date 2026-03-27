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
/// Performs the Auth0 login once per account type and caches the storage state
/// for subsequent tests.
/// </summary>
public static class AuthStateManager
{
	private static readonly SemaphoreSlim _userStateLock = new(1, 1);
	private static readonly SemaphoreSlim _adminStateLock = new(1, 1);
	private static string? _userStateFilePath;
	private static string? _adminStateFilePath;

	/// <summary>
	/// Returns the path to the cached Playwright storage state file after performing Auth0 login
	/// for the standard <b>User</b> role account.
	/// Returns <see langword="null"/> if <c>PLAYWRIGHT_TEST_EMAIL</c> or
	/// <c>PLAYWRIGHT_TEST_PASSWORD</c> environment variables are not set.
	/// </summary>
	/// <param name="page">A browser page used to perform the login flow.</param>
	/// <param name="baseUrl">The base URL of the application.</param>
	/// <returns>Path to the JSON storage-state file, or <see langword="null"/> if credentials are absent.</returns>
	public static async Task<string?> GetStorageStatePathAsync(IPage page, string baseUrl)
	{
		var email = Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_EMAIL");
		var password = Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_PASSWORD");

		if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
		{
			return null;
		}

		await _userStateLock.WaitAsync();
		try
		{
			if (_userStateFilePath is not null)
			{
				return _userStateFilePath;
			}

			_userStateFilePath = await PerformLoginAsync(
				page, baseUrl, email, password,
				"issuetracker-playwright-auth.json");

			return _userStateFilePath;
		}
		finally
		{
			_userStateLock.Release();
		}
	}

	/// <summary>
	/// Returns the path to the cached Playwright storage state file after performing Auth0 login
	/// for the <b>Admin</b> role account.
	/// Returns <see langword="null"/> if <c>PLAYWRIGHT_TEST_ADMIN_EMAIL</c> or
	/// <c>PLAYWRIGHT_TEST_ADMIN_PASSWORD</c> environment variables are not set.
	/// </summary>
	/// <param name="page">A browser page used to perform the login flow.</param>
	/// <param name="baseUrl">The base URL of the application.</param>
	/// <returns>Path to the JSON storage-state file, or <see langword="null"/> if credentials are absent.</returns>
	public static async Task<string?> GetAdminStorageStatePathAsync(IPage page, string baseUrl)
	{
		var email = Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_ADMIN_EMAIL");
		var password = Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_ADMIN_PASSWORD");

		if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
		{
			return null;
		}

		await _adminStateLock.WaitAsync();
		try
		{
			if (_adminStateFilePath is not null)
			{
				return _adminStateFilePath;
			}

			_adminStateFilePath = await PerformLoginAsync(
				page, baseUrl, email, password,
				"issuetracker-playwright-admin-auth.json");

			return _adminStateFilePath;
		}
		finally
		{
			_adminStateLock.Release();
		}
	}

	private static async Task<string> PerformLoginAsync(
		IPage page,
		string baseUrl,
		string email,
		string password,
		string stateFileName)
	{
		var loginUrl = $"{baseUrl.TrimEnd('/')}/account/login?returnUrl=/";
		await page.GotoAsync(loginUrl);
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Auth0 Universal Login: /authorize redirects via JS to /u/login where the form lives.
		await page.WaitForURLAsync(
			url => url.Contains("/u/login"),
			new PageWaitForURLOptions { Timeout = 30_000 });
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Auth0 Universal Login may use different selectors; try the most common ones.
		// First try the username field (classic + new Universal Login), then email input.
		var usernameSelector = "input[name=\"username\"], input[id=\"username\"], input[type=\"email\"]";
		await page.WaitForSelectorAsync(usernameSelector, new PageWaitForSelectorOptions { Timeout = 30_000 });

		// Step 1: fill email and submit (Auth0 identifier-first UL shows password on next step)
		await page.FillAsync(usernameSelector, email);
		await page.ClickAsync("button[type=\"submit\"]");

		// Step 2: password field becomes visible — wait for it, fill it, then submit
		try
		{
			await page.WaitForSelectorAsync(
				"input[name=\"password\"]",
				new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });
			await page.FillAsync("input[name=\"password\"]", password);
			await page.ClickAsync("button[type=\"submit\"]");
		}
		catch (TimeoutException)
		{
			// Classic UL shows both fields at once and submit may already be navigating — proceed
		}

		// Step 3: handle Auth0 consent screen (first-time login only).
		// Auth0 stores consent server-side; once accepted, this screen never appears again.
		// NOTE: You can eliminate this permanently by enabling "Skip User Consent" for the
		// application in the Auth0 Dashboard (Application → scroll to bottom → toggle off).
		if (page.Url.Contains("/u/consent"))
		{
			// Auth0 consent page has two buttons: <button name="action" value="accept">Allow</button>
			// and <button name="action" value="deny">Deny</button>. Use [value="accept"] to be
			// unambiguous — generic [name="action"] would match Deny if it appears first in the DOM.
			await page.ClickAsync("button[value=\"accept\"]");
		}

		// After the cross-origin redirect (auth0.com → localhost), Playwright's navigation-event
		// helpers (WaitForURLAsync / WaitForNavigationAsync) can throw "navigated to X" because
		// the OIDC middleware immediately issues a second redirect (/callback → home), causing a
		// frame-context race. Polling page.Url is simpler and more reliable here.
		// We must wait past /callback (not just reach it) because auth cookies arrive with the
		// 302 redirect response — they are not yet set while the browser is at /callback.
		var deadline = DateTime.UtcNow.AddSeconds(30);
		while (!page.Url.StartsWith(baseUrl) || page.Url.Contains("/callback"))
		{
			if (DateTime.UtcNow >= deadline)
				throw new TimeoutException($"Timed out waiting for post-login redirect to '{baseUrl}'. Current URL: '{page.Url}'");
			await Task.Delay(200);
		}

		// URL is on the app home page; wait for it to fully settle.
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 30_000 });

		if (!page.Url.StartsWith(baseUrl))
		{
			throw new InvalidOperationException(
				$"Login did not redirect back to the app. Expected URL starting with '{baseUrl}' but got '{page.Url}'. " +
				"Check credentials and Auth0 Allowed Callback URLs.");
		}

		var path = Path.Combine(Path.GetTempPath(), stateFileName);

		await page.Context.StorageStateAsync(new BrowserContextStorageStateOptions
		{
			Path = path
		});

		return path;
	}
}
