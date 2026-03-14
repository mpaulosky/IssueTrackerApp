// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserInfo.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Models;

/// <summary>
///   UserInfo value object - embedded sub-document for user references in MongoDB.
///   Replaces UserDto as the embedded type within domain models.
/// </summary>
[Serializable]
public sealed class UserInfo
{
	/// <summary>
	///   Gets or sets the identifier (Auth0 user ID).
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the name.
	/// </summary>
	/// <value>
	///   The name.
	/// </value>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the email address.
	/// </summary>
	/// <value>
	///   The email address.
	/// </value>
	public string Email { get; set; } = string.Empty;

	/// <summary>
	///   Gets an empty UserInfo instance.
	/// </summary>
	public static UserInfo Empty => new();
}
