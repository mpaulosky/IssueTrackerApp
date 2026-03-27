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

		await page.FillAsync("input[name=\"username\"]", email);
		await page.FillAsync("input[name=\"password\"]", password);
		await page.ClickAsync("button[type=\"submit\"]");

		await page.WaitForURLAsync(
			url => url.StartsWith(baseUrl),
			new PageWaitForURLOptions { Timeout = 30_000 });

		var path = Path.Combine(Path.GetTempPath(), stateFileName);

		await page.Context.StorageStateAsync(new BrowserContextStorageStateOptions
		{
			Path = path
		});

		return path;
	}
}
