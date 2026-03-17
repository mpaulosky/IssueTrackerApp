using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;

namespace Web.Auth;

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
			_logger.LogWarning(
				"Auth0:RoleClaimNamespace is not configured. Role-based authorization may not work. " +
				"Configure this setting to match your Auth0 tenant's role claim namespace.");
		}
	}

	/// <summary>
	/// Transforms the user's claims by mapping Auth0 custom role claims to standard role claims.
	/// </summary>
	public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
	{
		// Skip transformation if namespace is not configured
		if (string.IsNullOrEmpty(_roleClaimNamespace))
		{
			return Task.FromResult(principal);
		}

		// Check if we've already transformed (avoid duplicate transformations)
		if (principal.Identity is ClaimsIdentity identity && identity.IsAuthenticated)
		{
			// Look for Auth0 role claims (can be a single string or JSON array)
			var auth0RoleClaims = principal.FindAll(_roleClaimNamespace).ToList();

			if (auth0RoleClaims.Count == 0)
			{
				_logger.LogDebug("No Auth0 role claims found with namespace '{Namespace}'.", _roleClaimNamespace);
				return Task.FromResult(principal);
			}

			// Check if we've already added standard role claims to avoid duplication
			var hasStandardRoleClaims = principal.HasClaim(c => c.Type == ClaimTypes.Role);
			if (hasStandardRoleClaims)
			{
				// Already transformed
				return Task.FromResult(principal);
			}

			// Add standard role claims for each Auth0 role
			foreach (var roleClaim in auth0RoleClaims)
			{
				var roleValue = roleClaim.Value;

				// Auth0 may send roles as JSON array: ["Admin", "User"]
				// or as comma-separated string: "Admin,User"
				// or as single value: "Admin"
				if (roleValue.StartsWith('[') && roleValue.EndsWith(']'))
				{
					// Parse JSON array
					try
					{
						var roles = System.Text.Json.JsonSerializer.Deserialize<string[]>(roleValue);
						if (roles != null)
						{
							foreach (var role in roles)
							{
								identity.AddClaim(new Claim(ClaimTypes.Role, role));
								_logger.LogDebug("Mapped Auth0 role '{Role}' to standard role claim.", role);
							}
						}
					}
					catch (System.Text.Json.JsonException ex)
					{
						_logger.LogWarning(ex, "Failed to parse Auth0 role claim as JSON array: {Value}", roleValue);
					}
				}
				else if (roleValue.Contains(','))
				{
					// Handle comma-separated roles
					var roles = roleValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					foreach (var role in roles)
					{
						identity.AddClaim(new Claim(ClaimTypes.Role, role));
						_logger.LogDebug("Mapped Auth0 role '{Role}' to standard role claim.", role);
					}
				}
				else
				{
					// Single role value
					identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
					_logger.LogDebug("Mapped Auth0 role '{Role}' to standard role claim.", roleValue);
				}
			}

			_logger.LogInformation(
				"Transformed {Count} Auth0 role claim(s) for user '{UserId}'.",
				auth0RoleClaims.Count,
				principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown");
		}

		return Task.FromResult(principal);
	}
}
