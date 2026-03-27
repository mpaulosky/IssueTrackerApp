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

using Microsoft.Extensions.Configuration;

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
		// Forward Auth0 credentials to child processes (web subprocess runs in "Testing"
		// environment where user secrets are not auto-loaded, so we propagate them via
		// environment variables which are inherited by all Aspire-launched subprocesses).
		ForwardAuth0ConfigToChildProcesses();

		var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
				args: [],
				configureBuilder: static (options, _) =>
				{
					options.DisableDashboard = false;
				});

		builder.Configuration["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true";

		// Fix the web project's HTTPS port so the Auth0 callback URL is predictable.
		// Auth0 Allowed Callback URLs must include: https://localhost:7043/callback
		FixWebEndpointPort(builder, "https", 7043);

		App = await builder.BuildAsync();
		await App.StartAsync();
	}

	/// <summary>
	/// Reads Auth0 configuration from this test project's user secrets (or existing environment
	/// variables) and sets them as process-level environment variables so that Aspire-launched
	/// subprocesses (e.g. the <c>web</c> service) inherit them.
	/// </summary>
	private static void ForwardAuth0ConfigToChildProcesses()
	{
		// Build a config from the test project's own user secrets + current env vars
		var localConfig = new ConfigurationBuilder()
			.AddUserSecrets(typeof(AspireManager).Assembly, optional: true)
			.AddEnvironmentVariables()
			.Build();

		var auth0Keys = new[] { "Domain", "ClientId", "ClientSecret", "RoleClaimNamespace" };
		foreach (var key in auth0Keys)
		{
			var envKey = $"Auth0__{key}";
			// Skip if already set as an environment variable (CI sets these directly)
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envKey)))
				continue;

			var value = localConfig[$"Auth0:{key}"];
			if (!string.IsNullOrEmpty(value))
				Environment.SetEnvironmentVariable(envKey, value);
		}
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
