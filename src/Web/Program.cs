using System.Security.Claims;

using Auth0.AspNetCore.Authentication;

using Azure.Identity;

using Domain;
using Domain.Abstractions;
using Domain.Behaviors;
using Domain.Features.Issues.Commands.Bulk;
using Domain.Models;

using MediatR;

using FluentValidation;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using Persistence.AzureStorage;
using Persistence.MongoDb;

using Web.Auth;
using Web.Components;
using Web.Data;
using Web.Endpoints;
using Web.Features;
using Web.Helpers;
using Web.Hubs;
using Web.Services;
using Web.Testing;

var builder = WebApplication.CreateBuilder(args);

// In Testing mode (launched by AppHost.Tests), the app runs from build output rather than published
// output. Static web assets are only auto-loaded in Development, so enable them explicitly here so
// that Blazor interactive mode, scoped CSS, and JS assets are served correctly during E2E tests.
if (builder.Environment.IsEnvironment("Testing"))
{
	builder.WebHost.UseStaticWebAssets();
}

// Configure JSON serialization for MongoDB ObjectId
builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.Converters.Add(new ObjectIdJsonConverter());
});

// Add Azure Key Vault configuration for non-Development environments
if (!builder.Environment.IsDevelopment())
{
	var keyVaultUri = builder.Configuration["KeyVault:Uri"];
	if (!string.IsNullOrEmpty(keyVaultUri))
	{
		builder.Configuration.AddAzureKeyVault(
			new Uri(keyVaultUri),
			new DefaultAzureCredential());
	}
}

// Add service defaults (OpenTelemetry, service discovery, resilience, health checks)
builder.AddServiceDefaults();

// Add MongoDB persistence layer — or lightweight in-memory fakes for E2E testing
if (!builder.Environment.IsEnvironment("Testing"))
{
	builder.Services.AddMongoDbPersistence(builder.Configuration);
}
else
{
	RegisterFakeRepositories(builder.Services);
}

// Add MongoDB connection from Aspire service discovery (skip in test environment —
// the EF Core provider handles the connection, and Aspire service discovery hangs without AppHost)
if (!builder.Environment.IsEnvironment("Testing"))
{
	builder.AddMongoDBClient("mongodb");
}

// Add MediatR for CQRS pattern
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DomainMarker).Assembly));

// Register ValidationBehavior as a MediatR pipeline behavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Add FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(DomainMarker).Assembly);

// Add application services
builder.Services.AddScoped<IIssueService, IssueService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure Email Service (SendGrid or SMTP fallback)
var sendGridApiKey = builder.Configuration["SendGrid:ApiKey"];
if (!string.IsNullOrEmpty(sendGridApiKey))
{
	// Use SendGrid if an API key is configured
	builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("SendGrid"));
	builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
}
else
{
	// Fallback to SMTP for development/testing
	builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
	builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
}

// Add Email Queue Background Service (skip in Testing and IntegrationTesting — it polls MongoDB every 10 s)
if (!builder.Environment.IsEnvironment("Testing") && !builder.Environment.IsEnvironment("IntegrationTesting"))
{
	builder.Services.AddHostedService<EmailQueueBackgroundService>();
}

// Configure File Storage (Azure Blob or Local)
var blobConnectionString = builder.Configuration["BlobStorage:ConnectionString"];
if (!string.IsNullOrEmpty(blobConnectionString))
{
	// Use Azure Blob Storage if the connection string is configured
	builder.Services.AddAzureBlobStorage(builder.Configuration);
}
else
{
	// Fallback to local file storage for development
	builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}

// Add real-time notification services
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<SignalRClientService>();

// Add memory cache for undo service
builder.Services.AddMemoryCache();

// Add bulk operations services
builder.Services.AddScoped<BulkSelectionState>();
builder.Services.AddSingleton<InMemoryBulkOperationQueue>();
builder.Services.AddSingleton<IBulkOperationQueue>(sp =>
	sp.GetRequiredService<InMemoryBulkOperationQueue>());
builder.Services.AddScoped<IUndoService, InMemoryUndoService>();
builder.Services.AddScoped<IBulkOperationService, BulkOperationService>();

// Skip the bulk background worker in Testing and IntegrationTesting — it uses IRepository<Issue> and runs continuously
if (!builder.Environment.IsEnvironment("Testing") && !builder.Environment.IsEnvironment("IntegrationTesting"))
{
	builder.Services.AddHostedService<BulkOperationBackgroundService>();
}

// Register bulk operation handlers for background processing
builder.Services.AddScoped<BulkUpdateStatusCommandHandler>();
builder.Services.AddScoped<BulkUpdateCategoryCommandHandler>();
builder.Services.AddScoped<BulkAssignCommandHandler>();
builder.Services.AddScoped<BulkDeleteCommandHandler>();

// Add data seeder
builder.Services.AddDataSeeder();

// Configure authentication — Cookie-only in Testing mode; Auth0 OIDC in all other environments
if (builder.Environment.IsEnvironment("Testing"))
{
	// Use simple Cookie auth for E2E testing — no Auth0, no external dependencies.
	// Tests authenticate by navigating to GET /test/login?role=user|admin which sets the cookie.
	builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
		.AddCookie(opts => opts.LoginPath = "/test/login");
}
else
{
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

	// Register Auth0 claims transformation for role mapping
	// This maps Auth0's custom role claims (e.g., "https://issuetracker.com/roles")
	// to ASP.NET Core's standard ClaimTypes.Role so RequireRole() works correctly
	builder.Services.AddScoped<IClaimsTransformation, Auth0ClaimsTransformation>();
}

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
		policy.RequireRole(AuthorizationRoles.Admin));

	options.AddPolicy(AuthorizationPolicies.UserPolicy, policy =>
		policy.RequireRole(AuthorizationRoles.User));
});

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

// Add services to the container.
builder.Services.AddRazorComponents()
		.AddInteractiveServerComponents();

// Configure cascading authentication state
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Initialize MongoDB database (skip in Testing and IntegrationTesting environments - tests manage their own data)
if (!app.Environment.IsEnvironment("Testing") && !app.Environment.IsEnvironment("IntegrationTesting"))
{
	await app.Services.InitializeMongoDbAsync();

	// Seed default data
	await app.Services.SeedDataAsync();
}

// Map health check endpoints (development only by default)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
// StatusCodePagesWithReExecute re-executes requests to /not-found (a Blazor page, GET-only).
// This interferes with API PUT/DELETE responses by converting 401→405, so skip in Testing and IntegrationTesting.
if (!app.Environment.IsEnvironment("Testing") && !app.Environment.IsEnvironment("IntegrationTesting"))
{
	app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
}
app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
		.AddInteractiveServerRenderMode();

// Map SignalR hub endpoint
app.MapHub<IssueHub>("/hubs/issues");

// Map API endpoints
app.MapAttachmentEndpoints();

// Map API endpoints
app.MapCategoryEndpoints();

// Map Comment API endpoints
app.MapCommentEndpoints();

// Map Status API endpoints
app.MapStatusEndpoints();

// Map Auth0 login/logout endpoints
app.MapGet("/account/login", async (HttpContext context, IWebHostEnvironment env, string returnUrl = "/") =>
{
	// Validate returnUrl is local to prevent open redirect attacks
	// See: https://cheatsheetseries.owasp.org/cheatsheets/Unvalidated_Redirects_and_Forwards_Cheat_Sheet.html
	var validReturnUrl = !string.IsNullOrEmpty(returnUrl) && IsLocalUrl(returnUrl)
		? returnUrl
		: "/";

	if (env.IsEnvironment("Testing"))
	{
		// Redirect to test login endpoint so the cookie-auth challenge resolves gracefully
		return Results.Redirect($"/test/login?role=user&returnUrl={Uri.EscapeDataString(validReturnUrl)}");
	}

	var authenticationProperties = new AuthenticationProperties { RedirectUri = validReturnUrl };
	await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
	return Results.Empty;
}).AllowAnonymous();

// Use POST for logout to prevent CSRF attacks
// Antiforgery validation is handled by UseAntiforgery() middleware for form submissions
app.MapPost("/account/logout", async (HttpContext context, IWebHostEnvironment env) =>
{
	var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };

	if (!env.IsEnvironment("Testing"))
	{
		await context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
	}

	await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}).RequireAuthorization();

// Testing-only: lightweight login endpoint that signs in via Cookie auth with fake claims.
// Playwright tests navigate here instead of going through the Auth0 OIDC flow.
if (app.Environment.IsEnvironment("Testing"))
{
	app.MapGet("/test/login", async (HttpContext ctx, string role = "user", string returnUrl = "/") =>
	{
		var isAdmin = role.Equals("admin", StringComparison.OrdinalIgnoreCase);

		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, isAdmin ? "auth0|test-admin" : "auth0|test-user"),
			new(ClaimTypes.Name,           isAdmin ? "Test Admin"       : "Test User"),
			new(ClaimTypes.Email,          isAdmin ? "admin@test.com"   : "user@test.com"),
			new(ClaimTypes.Role,           isAdmin ? "Admin"            : "User"),
		};

		var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		await ctx.SignInAsync(new ClaimsPrincipal(identity));

		var safeReturn = !string.IsNullOrEmpty(returnUrl) && IsLocalUrl(returnUrl) ? returnUrl : "/";
		return Results.Redirect(safeReturn);
	}).AllowAnonymous();
}

// Helper method to validate local URLs (prevents open redirect)
static bool IsLocalUrl(string url)
{
	if (string.IsNullOrEmpty(url))
	{
		return false;
	}

	// Reject URLs with protocol schemes (http://, https://, //, etc.)
	if (url.StartsWith("//", StringComparison.Ordinal) ||
			url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
	{
		return false;
	}

	// Accept relative URLs that start with /
	return url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal);
}

// Registers lightweight in-memory fake repositories for all entity types.
// Used in the Testing environment instead of the real MongoDB-backed repositories.
static void RegisterFakeRepositories(IServiceCollection services)
{
	var issues = new FakeRepository<Issue>(FakeSeedData.Issues);
	var statuses = new FakeRepository<Status>(FakeSeedData.Statuses);
	var categories = new FakeRepository<Category>(FakeSeedData.Categories);

	services.AddSingleton<IRepository<Issue>>(issues);
	services.AddSingleton<IRepository<Status>>(statuses);
	services.AddSingleton<IRepository<Category>>(categories);
	services.AddSingleton<IRepository<Comment>>(new FakeRepository<Comment>());
	services.AddSingleton<IRepository<Attachment>>(new FakeRepository<Attachment>());
	services.AddSingleton<IRepository<EmailQueueItem>>(new FakeRepository<EmailQueueItem>());
}

app.Run();

// Make the implicit Program class public for WebApplicationFactory in tests
public partial class Program { }
