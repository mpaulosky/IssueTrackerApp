namespace Web.Auth;

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
