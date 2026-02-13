// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     AuthenticationStateProviderHelpers.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

using System.Security.Claims;

namespace Web.Helpers;

/// <summary>
///   AuthenticationStateProviderHelpers class
/// </summary>
public static class AuthenticationStateProviderHelpers
{
	/// <summary>
	///   Gets the user from authentication.
	/// </summary>
	/// <param name="provider">The AuthenticationState provider.</param>
	/// <param name="userData">The user service.</param>
	/// <returns>Task of Type User</returns>
	public static async Task<global::Shared.Models.User> GetUserFromAuth(
		this AuthenticationStateProvider provider,
		IUserService userData)
	{
		AuthenticationState authState = await provider.GetAuthenticationStateAsync();

		string? objectId = authState.User.Claims
			.FirstOrDefault(c => c.Type.Contains("objectidentifier"))?.Value;

		return await userData.GetUserFromAuthentication(objectId);
	}

	/// <summary>
	///   Is User Authorized Async method
	/// </summary>
	/// <param name="provider"></param>
	/// <returns>Task bool</returns>
	public static async Task<bool> IsUserAdminAsync(
		this AuthenticationStateProvider provider)
	{
		AuthenticationState authState = await provider.GetAuthenticationStateAsync();
		ClaimsPrincipal user = authState.User;

		string? jobTitle = user.Claims
			.FirstOrDefault(c => c.Type.Contains("jobTitle"))?.Value;


		return string.Equals(jobTitle, "Admin", StringComparison.Ordinal);
	}
}
