namespace Web.Auth;

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
