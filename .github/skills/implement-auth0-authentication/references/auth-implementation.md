# Core Implementation Code Patterns

This reference provides the complete code for Auth0 infrastructure classes and components.

Replace `YourApp` with your web project's root namespace in the snippets below.

## Auth Infrastructure Files

### Auth/Auth0Options.cs

Configuration model for Auth0 web application settings.

```csharp
namespace YourApp.Auth;

/// <summary>
/// Configuration options for Auth0 authentication.
/// </summary>
public sealed class Auth0Options
{
	/// <summary>
	/// Gets or sets the Auth0 domain (e.g., your-tenant.auth0.com).
	/// </summary>
	public string Domain { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the Auth0 client ID for this application.
	/// </summary>
	public string ClientId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the Auth0 client secret for this application.
	/// </summary>
	public string ClientSecret { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the custom namespace for Auth0 role claims.
	/// Example: "https://issuetracker.com/roles"
	/// This must match the claim namespace configured in your Auth0 tenant (Action/Rule).
	/// </summary>
	public string RoleClaimNamespace { get; set; } = string.Empty;
}
```

### Auth/AuthorizationRoles.cs

Role name constants.

```csharp
namespace YourApp.Auth;

/// <summary>
/// Defines role names used in authorization.
/// These roles should match the roles configured in Auth0.
/// </summary>
public static class AuthorizationRoles
{
	/// <summary>
	/// Admin role with full access to the application.
	/// </summary>
	public const string Admin = "Admin";

	/// <summary>
	/// Standard user role with basic access.
	/// </summary>
	public const string User = "User";
}
```

### Auth/AuthorizationPolicies.cs

Authorization policy name constants.

```csharp
namespace YourApp.Auth;

/// <summary>
/// Defines authorization policy names for the application.
/// </summary>
public static class AuthorizationPolicies
{
	/// <summary>
	/// Policy name for users with the Admin role.
	/// </summary>
	public const string AdminPolicy = "AdminPolicy";

	/// <summary>
	/// Policy name for users with the User role.
	/// </summary>
	public const string UserPolicy = "UserPolicy";
}
```

### Auth/Auth0ClaimsTransformation.cs

Claims transformation service that maps Auth0's custom role claims to ASP.NET Core's standard role claim type.

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace YourApp.Auth;

/// <summary>
/// Transforms Auth0 custom role claims to ASP.NET Core standard role claims.
/// Auth0 sends roles in a namespaced claim (e.g., "https://issuetracker.com/roles"),
/// but ASP.NET Core's RequireRole() expects claims with type ClaimTypes.Role.
/// This transformation maps Auth0 roles to the standard claim type.
/// </summary>
public sealed class Auth0ClaimsTransformation : IClaimsTransformation
{
	private readonly string _roleClaimNamespace;
	private readonly ILogger<Auth0ClaimsTransformation> _logger;

	public Auth0ClaimsTransformation(
		IConfiguration configuration,
		ILogger<Auth0ClaimsTransformation> logger)
	{
		_logger = logger;

		// Get the Auth0 role claim namespace from configuration
		var auth0Options = configuration.GetSection("Auth0").Get<Auth0Options>();
		_roleClaimNamespace = auth0Options?.RoleClaimNamespace ?? string.Empty;

		if (string.IsNullOrEmpty(_roleClaimNamespace))
		{
			_logger.LogInformation(
				"Auth0:RoleClaimNamespace is not configured. " +
				"Will fall back to reading the standard 'roles' JWT claim for role mapping.");
		}
	}

	/// <summary>
	/// Transforms the user's claims by mapping Auth0 custom role claims to standard role claims.
	/// Pass 1: uses the configured namespace claim type.
	/// Pass 2: falls back to the bare "roles" JWT claim.
	/// Pass 3: auto-detects any namespaced claim type ending in "/roles" when Passes 1 and 2
	///         find nothing, guarding against misconfigured Auth0:RoleClaimNamespace.
	/// </summary>
	public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
	{
		if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
			return Task.FromResult(principal);

		var rolesAdded = 0;

		// Pass 1: use configured namespace (e.g., "https://issuetracker.com/roles")
		if (!string.IsNullOrEmpty(_roleClaimNamespace))
		{
			var auth0RoleClaims = principal.FindAll(_roleClaimNamespace).ToList();
			rolesAdded += MapRoleClaims(identity, auth0RoleClaims);
		}

		// Pass 2: fallback — read standard "roles" JWT claim when namespace is absent
		if (rolesAdded == 0)
		{
			var standardRoleClaims = principal.FindAll("roles").ToList();
			rolesAdded += MapRoleClaims(identity, standardRoleClaims);
		}

		// Pass 3: auto-detect — scan for any namespaced role claim type when Passes 1 & 2 found nothing
		if (rolesAdded == 0)
		{
			var autoDetectedClaims = principal.Claims
				.Where(c => IsLikelyRoleClaimType(c.Type))
				.ToList();

			if (autoDetectedClaims.Count > 0)
			{
				_logger.LogInformation(
					"Auto-detected role claim type(s): {Types}. Consider setting Auth0:RoleClaimNamespace.",
					string.Join(", ", autoDetectedClaims.Select(c => c.Type).Distinct()));

				rolesAdded += MapRoleClaims(identity, autoDetectedClaims);
			}
		}

		if (rolesAdded > 0)
		{
			_logger.LogDebug(
				"Transformed {Count} role claim(s) for user '{UserId}'.",
				rolesAdded,
				principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown");
		}

		return Task.FromResult(principal);
	}

	/// <summary>
	/// Returns true when claimType looks like a namespaced Auth0 role claim.
	/// </summary>
	private static bool IsLikelyRoleClaimType(string claimType)
	{
		// Skip standard claim types already checked in Passes 1 and 2
		if (claimType.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) return false;
		if (claimType.Equals("roles", StringComparison.OrdinalIgnoreCase)) return false;
		// Match namespaced role claims like "https://*/roles"
		return claimType.EndsWith("/roles", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Maps role claims (from any source) to standard ASP.NET Core role claims.
	/// Handles multiple role formats: JSON arrays, comma-separated strings, or single values.
	/// </summary>
	private int MapRoleClaims(ClaimsIdentity identity, List<Claim> roleClaims)
	{
		var added = 0;
		foreach (var roleClaim in roleClaims)
		{
			var roleValue = roleClaim.Value;

			if (roleValue.StartsWith('[') && roleValue.EndsWith(']'))
			{
				try
				{
					var roles = System.Text.Json.JsonSerializer.Deserialize<string[]>(roleValue);
					if (roles is not null)
					{
						foreach (var role in roles)
						{
							if (!identity.HasClaim(ClaimTypes.Role, role))
							{
								identity.AddClaim(new Claim(ClaimTypes.Role, role));
								added++;
								_logger.LogDebug("Mapped role '{Role}' to standard role claim.", role);
							}
						}
					}
				}
				catch (System.Text.Json.JsonException ex)
				{
					_logger.LogWarning(ex, "Failed to parse role claim as JSON array: {Value}", roleValue);
				}
			}
			else if (roleValue.Contains(','))
			{
				var roles = roleValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var role in roles)
				{
					if (!identity.HasClaim(ClaimTypes.Role, role))
					{
						identity.AddClaim(new Claim(ClaimTypes.Role, role));
						added++;
						_logger.LogDebug("Mapped role '{Role}' to standard role claim.", role);
					}
				}
			}
			else
			{
				// Skip empty or whitespace-only role values
				if (string.IsNullOrWhiteSpace(roleValue))
					continue;

				if (!identity.HasClaim(ClaimTypes.Role, roleValue))
				{
					identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
					added++;
					_logger.LogDebug("Mapped role '{Role}' to standard role claim.", roleValue);
				}
			}
		}
		return added;
	}
}
```

## UI Components

### Components/Layout/LoginDisplay.razor

Login/logout UI with user greeting and profile link.

```razor
@inject NavigationManager Navigation

@{
	var currentPath = Navigation.ToBaseRelativePath(Navigation.Uri);
	var returnUrl = string.IsNullOrWhiteSpace(currentPath) ? "/" : $"/{currentPath}";
}

<AuthorizeView>
	<Authorized>
		<div class="flex items-center gap-1">
			<a href="/profile" class="px-3 py-2 rounded hover:bg-primary-100 hover:text-primary-700 transition-colors text-sm font-medium">
				Hey @context.User.Identity?.Name!
			</a>
			<form method="post" action="/account/logout" data-enhance="false" class="inline">
				<AntiforgeryToken />
				<button type="submit" class="px-3 py-2 rounded hover:bg-red-100 hover:text-red-700 transition-colors text-sm font-medium">
					Log out
				</button>
			</form>
		</div>
	</Authorized>
	<NotAuthorized>
		<a href="/account/login?returnUrl=@Uri.EscapeDataString(returnUrl)" class="btn btn-primary transition-colors">Log in</a>
	</NotAuthorized>
</AuthorizeView>
```

**Usage**: Place in your navigation layout (e.g., `NavMenu.razor` or `MainLayout.razor`).

### Components/Layout/LoginComponent.razor

Minimal login/logout buttons.

```razor
@inject NavigationManager Navigation

@{
	var currentPath = Navigation.ToBaseRelativePath(Navigation.Uri);
	var returnUrl = string.IsNullOrWhiteSpace(currentPath) ? "/" : $"/{currentPath}";
}

<AuthorizeView>
	<Authorized>
		<form method="post" action="/account/logout" data-enhance="false" class="inline">
			<AntiforgeryToken />
			<button type="submit" class="btn btn-danger transition-colors">Log out</button>
		</form>
	</Authorized>
	<NotAuthorized>
		<a href="/account/login?returnUrl=@Uri.EscapeDataString(returnUrl)" class="btn btn-primary transition-colors">Log in</a>
	</NotAuthorized>
</AuthorizeView>
```

**Usage**: Alternative minimal version for simple layouts.

### Components/User/Profile.razor

User profile page displaying claims, roles, profile picture, and debug information.

```razor
@page "/profile"

@using System.Security.Claims
@using Microsoft.Extensions.Configuration
@attribute [Authorize]
@inject IConfiguration Configuration

<h1>User Profile</h1>

<div class="container mx-auto">

	<AuthorizeView>
		<Authorized>
			<div class="container-card">
				<h2 class="text-2xl mb-4">Profile Information</h2>

				<div class="grid grid-cols-1 md:grid-cols-3 gap-6 items-start">
					<div>
						<h3 class="text-lg mb-2">Basic Information</h3>
						<div class="space-y-2">
							<p class="font-bold">
								<span>Name:</span>
								<span>@_username</span>
							</p>
							<p class="font-bold">
								<span>Email:</span>
								<span>@_emailAddress</span>
							</p>
							<p class="font-bold">
								<span>User ID:</span>
								<span>@_userId</span>
							</p>
						</div>
					</div>

					<div>
						<h3 class="text-lg mb-2">Roles & Permissions</h3>
						<div class="space-y-2">
							@if (_roles.Any())
							{
								<p class="font-bold">
									<span>Roles:</span>
								</p>
								<ul class="list-disc list-inside ml-4">
									@foreach (var role in _roles)
									{
										<li class="font-bold">@role</li>
									}
								</ul>
							}
							else
							{
								<p>No roles assigned</p>
							}
						</div>
					</div>

					<div class="flex flex-col items-center justify-start">
						<span class="font-medium mb-2">Profile Picture:</span>
						@if (!string.IsNullOrEmpty(_picture))
						{
							<img src="@_picture" alt="Profile Picture"
									 class="rounded-full w-24 h-24 object-cover border-2" />
						}
						else
						{
							<div class="rounded-full w-24 h-24 bg-gray-700 flex items-center justify-center border-2">
								<span class="text-2xl">?</span>
							</div>
						}
					</div>
				</div>
			</div>

			<div class="container-card">
				<h2 class="text-2xl mb-4">All Claims</h2>
				<h4 class="text-sm mb-4">Debug information showing all claims for this user:</h4>

				<div class="overflow-x-auto">
					<table class="min-w-full divide-y">
						<thead>
							<tr>
								<th class="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider">Claim Type</th>
								<th class="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider">Value</th>
							</tr>
						</thead>
						<tbody class="divide-y">
							@foreach (var claim in context.User.Claims.OrderBy(c => c.Type))
							{
								<tr>
									<td class="px-6 py-4 whitespace-nowrap text-sm">@claim.Type</td>
									<td class="px-6 py-4 whitespace-nowrap text-sm">@claim.Value</td>
								</tr>
							}
						</tbody>
					</table>
				</div>
			</div>
		</Authorized>
	</AuthorizeView>
</div>

@code {
	[CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }

	private string _userId = "";
	private string _username = "";
	private string _emailAddress = "";
	private string _picture = "";
	private List<string> _roles = new();

	protected override async Task OnInitializedAsync()
	{
		if (AuthenticationState is not null)
		{
			var state = await AuthenticationState;

			_username = state.User.Identity?.Name ?? string.Empty;

			_userId = state.User.Claims
				.Where(c => c.Type.Equals(ClaimTypes.NameIdentifier))
				.Select(c => c.Value)
				.FirstOrDefault() ?? string.Empty;

			_emailAddress = state.User.Claims
				.Where(c => c.Type.Equals(ClaimTypes.Email))
				.Select(c => c.Value)
				.FirstOrDefault() ?? string.Empty;

			_picture = state.User.Claims
				.Where(c => c.Type.Equals("picture"))
				.Select(c => c.Value)
				.FirstOrDefault() ?? string.Empty;

			var roleNamespace = Configuration["Auth0:RoleClaimNamespace"] ?? string.Empty;
			_roles = GetAllRoleClaims(state.User, roleNamespace);
		}

		await base.OnInitializedAsync();
	}

	// Helper to get all role claims for a user
	private static List<string> GetAllRoleClaims(ClaimsPrincipal user, string? roleClaimNamespace = null)
	{
		var roleTypesList = new List<string> { ClaimTypes.Role, "role", "roles" };

		if (!string.IsNullOrWhiteSpace(roleClaimNamespace))
			roleTypesList.Add(roleClaimNamespace);

		return user.Claims
			.Where(c => roleTypesList.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
			.Select(c => c.Value)
			.Where(v => !string.IsNullOrWhiteSpace(v))
			.Distinct()
			.ToList();
	}
}
```

**Note**: Update CSS classes to match your application's styling framework (Tailwind, Bootstrap, etc.).

## Auth0 Action Example

To include roles in the ID token, create an Auth0 Action (Actions → Flows → Login):

```javascript
/**
* Handler that will be called during the execution of a PostLogin flow.
*
* @param {Event} event - Details about the user and the context in which they are logging in.
* @param {PostLoginAPI} api - Interface whose methods can be used to change the behavior of the login.
*/
exports.onExecutePostLogin = async (event, api) => {
  const namespace = 'https://yourapp.com';
  if (event.authorization) {
    api.idToken.setCustomClaim(`${namespace}/roles`, event.authorization.roles);
    api.accessToken.setCustomClaim(`${namespace}/roles`, event.authorization.roles);
  }
};
```

Replace `https://yourapp.com` with your actual namespace that matches the `Auth0:RoleClaimNamespace` configuration.
