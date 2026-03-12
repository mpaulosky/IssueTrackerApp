// Copyright (c) IssueTrackerApp. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

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

		builder.ConfigureServices(services =>
		{
			// Remove MongoDB-related services that require actual database connection
			RemoveServicesByType(services, "MongoDB");
			RemoveServicesByType(services, "Mongo");
			RemoveServicesByType(services, "DbContext");
			RemoveServicesByType(services, "IssueTrackerDbContext");
			RemoveServicesByType(services, "IRepository");
			RemoveServicesByType(services, "Repository");
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
