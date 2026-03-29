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
			_logger.LogInformation(
				"Auth0:RoleClaimNamespace is not configured. " +
				"Will fall back to reading the standard 'roles' JWT claim for role mapping.");
		}
	}

	/// <summary>
	/// Transforms the user's claims by mapping Auth0 custom role claims to standard role claims.
	/// First attempts to use the configured namespace; if no roles are found or namespace is empty,
	/// falls back to reading the standard "roles" JWT claim.
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
		// Some Auth0 setups include roles directly without a custom namespace.
		if (rolesAdded == 0)
		{
			var standardRoleClaims = principal.FindAll("roles").ToList();
			rolesAdded += MapRoleClaims(identity, standardRoleClaims);
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
