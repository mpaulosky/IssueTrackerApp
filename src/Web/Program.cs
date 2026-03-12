using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Persistence.MongoDb;
using Web.Auth;
using Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, service discovery, resilience, health checks)
builder.AddServiceDefaults();

// Add MongoDB persistence layer
builder.Services.AddMongoDbPersistence(builder.Configuration);

// Add MongoDB connection from Aspire service discovery
builder.AddMongoDBClient("mongodb");

// Configure Auth0 authentication
var auth0Options = builder.Configuration.GetSection("Auth0").Get<Auth0Options>()
	?? throw new InvalidOperationException("Auth0 configuration is missing.");

builder.Services
	.AddAuth0WebAppAuthentication(options =>
	{
		options.Domain = auth0Options.Domain;
		options.ClientId = auth0Options.ClientId;
		options.ClientSecret = auth0Options.ClientSecret;
		// Use Authorization Code flow with PKCE for enhanced security
		options.Scope = "openid profile email";
	});

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
		policy.RequireRole(AuthorizationRoles.Admin));

	options.AddPolicy(AuthorizationPolicies.UserPolicy, policy =>
		policy.RequireRole(AuthorizationRoles.User));
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure cascading authentication state
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Initialize MongoDB database
await app.Services.InitializeMongoDbAsync();

// Map health check endpoints (development only by default)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Auth0 login/logout endpoints
app.MapGet("/account/login", async (HttpContext context, string returnUrl = "/") =>
{
	var authenticationProperties = new AuthenticationProperties
	{
		RedirectUri = returnUrl
	};
	await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
}).AllowAnonymous();

app.MapGet("/account/logout", async (HttpContext context) =>
{
	var authenticationProperties = new AuthenticationProperties
	{
		RedirectUri = "/"
	};
	await context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
	await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}).RequireAuthorization();

app.Run();

// Make the implicit Program class public for WebApplicationFactory in tests
public partial class Program { }
