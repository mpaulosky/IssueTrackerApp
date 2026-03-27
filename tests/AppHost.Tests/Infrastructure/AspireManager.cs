// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AspireManager.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AppHost.Tests.Infrastructure;

/// <summary>
/// Startup and configure the Aspire application for testing.
/// </summary>
public class AspireManager : IAsyncLifetime
{

	internal PlaywrightManager PlaywrightManager { get; } = new();

	internal DistributedApplication? App { get; private set; }

	/// <summary>
	/// Starts the <see cref="Projects.AppHost"/> Aspire application.  Called once by
	/// <see cref="InitializeAsync"/> before any test in the collection executes.
	/// </summary>
	private async Task StartAppAsync()
	{
		// Propagate ASPNETCORE_ENVIRONMENT=Testing to all Aspire-launched child processes.
		// In Testing mode the web app uses in-memory fake repositories, Cookie auth, and
		// skips background DB services — making E2E tests fast and self-contained.
		Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

		var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
				args: [],
				configureBuilder: static (options, _) =>
				{
					options.DisableDashboard = false;
				});

		builder.Configuration["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true";

		// Fix the web project's HTTPS port so the test base URL is predictable.
		FixWebEndpointPort(builder, "https", 7043);

		App = await builder.BuildAsync();
		await App.StartAsync();
	}

	/// <summary>
	/// Forces a fixed port on the named endpoint of the "web" resource so that the
	/// Auth0 <c>redirect_uri</c> is predictable across test runs.
	/// Setting <c>IsProxied = false</c> makes the app bind directly to <paramref name="port"/>
	/// without a DCP proxy in between, so both Playwright and the OIDC middleware see
	/// the same URL (<c>https://localhost:7043/callback</c>).
	/// </summary>
	private static void FixWebEndpointPort(IDistributedApplicationTestingBuilder builder, string scheme, int port)
	{
		var webResource = builder.Resources
			.OfType<IResourceWithEndpoints>()
			.FirstOrDefault(r => r.Name == "web");
		if (webResource is null) return;

		var endpoint = webResource.Annotations
			.OfType<EndpointAnnotation>()
			.FirstOrDefault(e => string.Equals(e.UriScheme, scheme, StringComparison.OrdinalIgnoreCase));
		if (endpoint is not null)
		{
			endpoint.Port = port;
			endpoint.IsProxied = false; // bind directly — no DCP proxy, app sees port 7043
		}
	}


	public async Task InitializeAsync()
	{
		await PlaywrightManager.InitializeAsync();
		await StartAppAsync();
	}
	public async Task DisposeAsync()
	{
		await PlaywrightManager.DisposeAsync();

		await (App?.DisposeAsync() ?? ValueTask.CompletedTask);
	}
}
