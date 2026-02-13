// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     AllServicesToRegister.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

using IssueTracker.UI.Extensions;

namespace Web.Extensions;

/// <summary>
///   RegisterServices class
/// </summary>
[ExcludeFromCodeCoverage]
public static class AllServicesToRegister
{
	/// <summary>
	///   Configures the service's method.
	/// </summary>
	/// <param name="builder">The builder.</param>
	/// <param name="config">ConfigurationManager</param>
	public static void ConfigureServices(this WebApplicationBuilder builder, ConfigurationManager config)
	{
		// Add services to the container.

		builder.Services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();

		builder.Services.AddAuthorizationService();

		builder.Services.AddAuthenticationService(config);

		builder.Services.RegisterConnections(config);

		builder.Services.RegisterDatabaseContext();

		builder.Services.AddHealthChecks().AddCheck<MongoHealthCheck>("MongoDbConnectionCheck");

		builder.Services.RegisterPlugInRepositories();

		builder.Services.RegisterServicesCollections();

		builder.Services.AddRazorPages();

		builder.Services.AddMemoryCache();

		builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();

		builder.Services.AddBlazoredSessionStorage();
	}
}