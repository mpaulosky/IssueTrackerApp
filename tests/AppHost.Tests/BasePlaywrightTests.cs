// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     BasePlaywrightTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Tests.Infrastructure;
using Microsoft.Playwright;

namespace AppHost.Tests;

/// <summary>
/// Base class for Playwright tests, providing common functionality and setup for Playwright testing with ASP.NET Core.
/// All derived classes share a single <see cref="AspireManager"/> instance via the
/// <see cref="AppHostTestCollection"/> collection fixture — AppHost starts once per test run.
/// </summary>
[Collection(AppHostTestCollection.Name)]
public abstract class BasePlaywrightTests : IAsyncDisposable
{

	protected BasePlaywrightTests(AspireManager aspireManager) =>
		AspireManager = aspireManager ?? throw new ArgumentNullException(nameof(aspireManager));

	AspireManager AspireManager { get; }
	PlaywrightManager PlaywrightManager => AspireManager.PlaywrightManager;

	// CI cold-start can take up to 2 min; local dev is typically ~10 s
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

	private IBrowserContext? _context;
	private IBrowserContext? _authContext;

	public async Task InteractWithPageAsync(string serviceName,
		Func<IPage, Task> test,
		ViewportSize? size = null)
	{
		var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;

		var endpoint = AspireManager.App?.GetEndpoint(serviceName, "https")
			?? throw new InvalidOperationException($"Service '{serviceName}' not found in the application endpoints");

		await AspireManager.App!.ResourceNotifications
			.WaitForResourceHealthyAsync(serviceName, cancellationToken)
			.WaitAsync(DefaultTimeout, cancellationToken);

		var page = await CreateNewPageAsync(endpoint, size);

		try
		{
			await test(page);
		}
		finally
		{
			await page.CloseAsync();
		}
	}

	private async Task<IPage> CreateNewPageAsync(Uri uri, ViewportSize? size = null)
	{
		_context = await PlaywrightManager.Browser.NewContextAsync(new BrowserNewContextOptions
		{
			IgnoreHTTPSErrors = true,
			ColorScheme = ColorScheme.Dark,
			ViewportSize = size,
			BaseURL = uri.ToString()
		});

		return await _context.NewPageAsync();

	}

	/// <summary>Creates an authenticated browser context page using stored Auth0 state.</summary>
	protected async Task<(IPage Page, bool HasAuth)> CreateAuthenticatedPageAsync(Uri uri) =>
		await CreateContextPageAsync(uri, AuthStateManager.GetStorageStatePathAsync);

	/// <summary>Creates an admin-authenticated browser context page using stored Auth0 admin state.</summary>
	protected async Task<(IPage Page, bool HasAuth)> CreateAdminAuthenticatedPageAsync(Uri uri) =>
		await CreateContextPageAsync(uri, AuthStateManager.GetAdminStorageStatePathAsync);

	private async Task<(IPage Page, bool HasAuth)> CreateContextPageAsync(
		Uri uri,
		Func<IPage, string, Task<string?>> getStatePath)
	{
		var loginContext = await PlaywrightManager.Browser.NewContextAsync(new BrowserNewContextOptions
		{
			IgnoreHTTPSErrors = true,
			BaseURL = uri.ToString()
		});
		var loginPage = await loginContext.NewPageAsync();

		var statePath = await getStatePath(loginPage, uri.ToString());
		await loginContext.CloseAsync();

		if (statePath is null)
		{
			return (await CreateNewPageAsync(uri), false);
		}

		_authContext = await PlaywrightManager.Browser.NewContextAsync(new BrowserNewContextOptions
		{
			IgnoreHTTPSErrors = true,
			ColorScheme = ColorScheme.Dark,
			StorageStatePath = statePath,
			BaseURL = uri.ToString()
		});

		return (await _authContext.NewPageAsync(), true);
	}

	/// <summary>Runs test with an authenticated page. Skips gracefully if credentials not configured.</summary>
	protected Task InteractWithAuthenticatedPageAsync(
		string serviceName,
		Func<IPage, Task> test,
		ViewportSize? size = null) =>
		InteractWithRolePageAsync(serviceName, test, adminRole: false, size);

	/// <summary>Runs test with an admin-authenticated page. Skips gracefully if admin credentials not configured.</summary>
	protected Task InteractWithAdminPageAsync(
		string serviceName,
		Func<IPage, Task> test,
		ViewportSize? size = null) =>
		InteractWithRolePageAsync(serviceName, test, adminRole: true, size);

	private async Task InteractWithRolePageAsync(
		string serviceName,
		Func<IPage, Task> test,
		bool adminRole,
		ViewportSize? size = null)
	{
		var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(120)).Token;

		var endpoint = AspireManager.App?.GetEndpoint(serviceName, "https")
			?? throw new InvalidOperationException($"Service '{serviceName}' not found");

		await AspireManager.App!.ResourceNotifications
			.WaitForResourceHealthyAsync(serviceName, cancellationToken)
			.WaitAsync(TimeSpan.FromSeconds(120), cancellationToken);

		var (page, hasAuth) = adminRole
			? await CreateAdminAuthenticatedPageAsync(endpoint)
			: await CreateAuthenticatedPageAsync(endpoint);

		if (!hasAuth)
		{
			// Credentials not configured — skip gracefully
			await page.CloseAsync();
			return;
		}

		try { await test(page); }
		finally { await page.CloseAsync(); }
	}


	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		if (_context is not null)
		{
			await _context.DisposeAsync();
		}

		if (_authContext is not null)
		{
			await _authContext.DisposeAsync();
		}
	}
}


