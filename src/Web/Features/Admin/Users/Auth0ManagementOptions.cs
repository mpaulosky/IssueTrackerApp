// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Auth0ManagementOptions.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

namespace Web.Features.Admin.Users;

/// <summary>
///   Configuration options for the Auth0 Machine-to-Machine (M2M) Management API application.
///   Bind from the <c>Auth0Management</c> configuration section.
/// </summary>
/// <remarks>
///   These credentials are distinct from the OIDC web-app credentials in <c>Auth0:ClientId</c> /
///   <c>Auth0:ClientSecret</c>. Store them in User Secrets (development) or Azure Key Vault (production).
///   Never commit real values to source control.
/// </remarks>
public sealed record Auth0ManagementOptions
{
	/// <summary>The configuration section name used to bind this record.</summary>
	public const string SectionName = "Auth0Management";

	/// <summary>Gets or sets the M2M application Client ID.</summary>
	public string ClientId { get; init; } = string.Empty;

	/// <summary>Gets or sets the M2M application Client Secret.</summary>
	public string ClientSecret { get; init; } = string.Empty;

	/// <summary>Gets or sets the Auth0 tenant domain (e.g. <c>your-tenant.auth0.com</c>).</summary>
	public string Domain { get; init; } = string.Empty;

	/// <summary>
	///   Gets or sets the Management API audience
	///   (e.g. <c>https://your-tenant.auth0.com/api/v2/</c>).
	/// </summary>
	public string Audience { get; init; } = string.Empty;
}
