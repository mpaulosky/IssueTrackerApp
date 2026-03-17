namespace Web.Auth;

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
