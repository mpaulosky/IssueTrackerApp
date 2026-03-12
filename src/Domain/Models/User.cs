// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     User.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

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
}
