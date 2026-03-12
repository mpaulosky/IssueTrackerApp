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
}
