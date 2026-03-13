// Copyright (c) IssueTrackerApp. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Tests;

/// <summary>
/// Custom WebApplicationFactory for testing that disables external dependencies
/// like MongoDB and Aspire service defaults.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		// Set environment to Testing to skip MongoDB initialization
		builder.UseEnvironment("Testing");

		// Add dummy configuration for Auth0
		builder.ConfigureAppConfiguration((context, config) =>
		{
			var testConfig = new Dictionary<string, string?>
			{
				["Auth0:Domain"] = "test.auth0.com",
				["Auth0:ClientId"] = "test-client-id",
				["Auth0:ClientSecret"] = "test-client-secret"
			};
			config.AddInMemoryCollection(testConfig);
		});

		builder.ConfigureServices(services =>
		{
			// Remove MongoDB-related services that require actual database connection
			RemoveServicesByType(services, "MongoDB");
			RemoveServicesByType(services, "Mongo");
			RemoveServicesByType(services, "DbContext");
			RemoveServicesByType(services, "IssueTrackerDbContext");
			RemoveServicesByType(services, "IRepository");
			RemoveServicesByType(services, "Repository");

			// Remove Auth0 authentication and replace with test authentication
			var authDescriptors = services
				.Where(d => d.ServiceType.FullName?.Contains("Auth0") == true ||
				            d.ServiceType.FullName?.Contains("OpenId") == true)
				.ToList();
			
			foreach (var descriptor in authDescriptors)
			{
				services.Remove(descriptor);
			}

			// Add test authentication scheme
			services.AddAuthentication("Test")
				.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
		});
	}

	private static void RemoveServicesByType(IServiceCollection services, string typeNameContains)
	{
		var descriptorsToRemove = services
			.Where(d => d.ServiceType.FullName?.Contains(typeNameContains) == true ||
			            d.ImplementationType?.FullName?.Contains(typeNameContains) == true ||
			            d.ServiceType.Name.Contains(typeNameContains) ||
			            (d.ImplementationType?.Name.Contains(typeNameContains) ?? false))
			.ToList();

		foreach (var descriptor in descriptorsToRemove)
		{
			services.Remove(descriptor);
		}
	}
}

/// <summary>
/// Test authentication handler for bypassing Auth0 in tests.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	public TestAuthHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder) : base(options, logger, encoder)
	{
	}

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		// Return unauthenticated for security tests (they test auth behavior)
		return Task.FromResult(AuthenticateResult.NoResult());
	}
}
