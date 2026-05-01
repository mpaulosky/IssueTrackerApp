# Program.cs Configuration

Complete `Program.cs` configuration for Auth0 authentication with the same secure patterns used in the current app.

## Required Using Statements

```csharp
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Web.Auth;
using Web.Features.Admin.Users;
```

## Authentication and Authorization Setup

Add this configuration after the other service registrations and before the Razor component setup:

```csharp
// Register Auth0 Management API user-management service when admin features are enabled.
builder.Services.AddUserManagement(builder.Configuration);

// Configure authentication — Cookie-only in Testing mode; Auth0 OIDC in all other environments
if (builder.Environment.IsEnvironment("Testing"))
{
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(opts => opts.LoginPath = "/test/login");
}
else
{
var auth0Options = builder.Configuration.GetSection("Auth0").Get<Auth0Options>()
?? throw new InvalidOperationException("Auth0 configuration is missing.");

builder.Services
.AddAuth0WebAppAuthentication(options =>
{
options.Domain = auth0Options.Domain;
options.ClientId = auth0Options.ClientId;
options.ClientSecret = auth0Options.ClientSecret;
options.Scope = "openid profile email";
});

builder.Services.AddScoped<IClaimsTransformation, Auth0ClaimsTransformation>();
}

builder.Services.AddAuthorization(options =>
{
options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
policy.RequireRole(AuthorizationRoles.Admin));

options.AddPolicy(AuthorizationPolicies.UserPolicy, policy =>
policy.RequireRole(AuthorizationRoles.User));
});
```

## Cascading Authentication State

```csharp
builder.Services.AddRazorComponents()
.AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
```

## Secure Login/Logout Endpoints

The Auth0 SDK handles `/callback`, but the current app maps explicit login/logout endpoints so it can validate `returnUrl`, support the testing environment, and use POST + antiforgery for logout.

```csharp
app.MapGet("/account/login", async (HttpContext context, IWebHostEnvironment env, string returnUrl = "/") =>
{
var validReturnUrl = !string.IsNullOrEmpty(returnUrl) && IsLocalUrl(returnUrl)
? returnUrl
: "/";

if (env.IsEnvironment("Testing"))
{
return Results.Redirect($"/test/login?role=user&returnUrl={Uri.EscapeDataString(validReturnUrl)}");
}

var authenticationProperties = new AuthenticationProperties { RedirectUri = validReturnUrl };
await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
return Results.Empty;
}).AllowAnonymous();

app.MapPost("/account/logout", async (HttpContext context, IWebHostEnvironment env) =>
{
var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };

if (!env.IsEnvironment("Testing"))
{
await context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
}

await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}).RequireAuthorization();
```

### Testing-Only Login Endpoint

```csharp
if (app.Environment.IsEnvironment("Testing"))
{
app.MapGet("/test/login", async (HttpContext ctx, string role = "user", string returnUrl = "/") =>
{
var isAdmin = role.Equals("admin", StringComparison.OrdinalIgnoreCase);

var claims = new List<Claim>
{
new(ClaimTypes.NameIdentifier, isAdmin ? "auth0|test-admin" : "auth0|test-user"),
new(ClaimTypes.Name, isAdmin ? "Test Admin" : "Test User"),
new(ClaimTypes.Email, isAdmin ? "admin@test.com" : "user@test.com"),
new(ClaimTypes.Role, isAdmin ? "Admin" : "User"),
};

var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
await ctx.SignInAsync(new ClaimsPrincipal(identity));

var safeReturn = !string.IsNullOrEmpty(returnUrl) && IsLocalUrl(returnUrl) ? returnUrl : "/";
return Results.Redirect(safeReturn);
}).AllowAnonymous();
}
```

### Local URL Validation Helper

```csharp
static bool IsLocalUrl(string url)
{
if (string.IsNullOrEmpty(url))
{
return false;
}

if (url.StartsWith("//", StringComparison.Ordinal) ||
url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
return false;
}

return url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal);
}
```

## Middleware Pipeline

Use explicit middleware registration rather than assuming the SDK inserted it for you:

```csharp
var app = builder.Build();

// ... exception handling, HTTPS, status-code pages ...

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
.AddInteractiveServerRenderMode();
```

## Complete Example

```csharp
using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Web.Auth;
using Web.Components;
using Web.Features.Admin.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUserManagement(builder.Configuration);

if (builder.Environment.IsEnvironment("Testing"))
{
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(opts => opts.LoginPath = "/test/login");
}
else
{
var auth0Options = builder.Configuration.GetSection("Auth0").Get<Auth0Options>()
?? throw new InvalidOperationException("Auth0 configuration is missing.");

builder.Services
.AddAuth0WebAppAuthentication(options =>
{
options.Domain = auth0Options.Domain;
options.ClientId = auth0Options.ClientId;
options.ClientSecret = auth0Options.ClientSecret;
options.Scope = "openid profile email";
});

builder.Services.AddScoped<IClaimsTransformation, Auth0ClaimsTransformation>();
}

builder.Services.AddAuthorization(options =>
{
options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
policy.RequireRole(AuthorizationRoles.Admin));
options.AddPolicy(AuthorizationPolicies.UserPolicy, policy =>
policy.RequireRole(AuthorizationRoles.User));
});

builder.Services.AddRazorComponents()
.AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
.AddInteractiveServerRenderMode();

app.Run();
```

## Environment Notes

### Testing

- Uses cookie-based auth only
- Avoids external Auth0 dependencies
- Supports `/test/login?role=user|admin`

### Development

- Usually reads Auth0 secrets from user secrets
- Callback URL is typically `https://localhost:5001/callback`

### Production

- Use Azure Key Vault, environment variables, or another secure secret store
- Ensure callback/logout URLs match the production hostname exactly

## Troubleshooting

### "Auth0 configuration is missing"

- Ensure the `Auth0` section exists
- Verify user secrets or environment variables are loaded

### Login Always Redirects Home

- The login endpoint only accepts local `returnUrl` values
- Build links with a base-relative path such as `/issues/123`, not `https://example.com/issues/123`

### Middleware Errors or 401s

- Ensure `UseAuthentication()` runs before `UseAuthorization()`
- Keep both before endpoint mapping
- Leave `UseAntiforgery()` enabled so logout POSTs stay protected
