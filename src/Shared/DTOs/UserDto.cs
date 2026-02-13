// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     BasicUserModel.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

using Shared.Models;

namespace Shared.Models.DTOs;

/// <summary>
///   UserDto class
/// </summary>
[Serializable]
public class UserDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="UserDto" /> class.
	/// </summary>
	public UserDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="UserDto" /> class.
	/// </summary>
	/// <param name="user">The user.</param>
	public UserDto(User user)
	{
		Id = user.Id;
		Email = user.Email;
		Name = user.Name;
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="UserDto" /> class.
	/// </summary>
	/// <param name="id">The identifier.</param>
	/// <param name="email">The email address.</param>
	/// <param name="name">The name.</param>
	public UserDto(
		string id,
		string name,
		string email) : this()
	{
		Id = id;
		Name = name;
		Email = email;
	}

	/// <summary>
	///   Gets the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	public string Id { get; init; } = string.Empty;

	/// <summary>
	///   Gets or sets the name.
	/// </summary>
	/// <value>
	///   The name.
	/// </value>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	///   Gets or sets the email address.
	/// </summary>
	/// <value>
	///   The email address.
	/// </value>
	public string Email { get; init; } = string.Empty;
}
