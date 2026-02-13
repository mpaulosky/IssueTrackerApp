// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     UserModel.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =============================================

namespace Shared.Models;

/// <summary>
///   User class
/// </summary>
[Serializable]
public class User
{
	/// <summary>
	///   Gets or sets the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the display name.
	/// </summary>
	/// <value>
	///   The display name.
	/// </value>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the email address.
	/// </summary>
	/// <value>
	///   The email address.
	/// </value>
	public string Email { get; set; } = string.Empty;

}
