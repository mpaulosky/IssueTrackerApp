// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UserDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =============================================

using Shared.Models;

namespace Shared.Models.DTOs;

/// <summary>
///   UserDto record for simplified user representation
/// </summary>
[Serializable]
public record UserDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="UserDto" /> record.
	/// </summary>
	public UserDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="UserDto" /> record.
	/// </summary>
	/// <param name="user">The user.</param>
	public UserDto(User user)
	{
		Id = user.Id;
		Name = user.Name;
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="UserDto" /> record.
	/// </summary>
	/// <param name="id">The identifier.</param>
	/// <param name="name">The name.</param>
	/// <param name="email">The email address.</param>
	public UserDto(string id, string name, string email) : this()
	{
		Id = id;
		Name = name;
		Email = email;
	}

	/// <summary>
	///   Gets or initializes the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	public string Id { get; init; } = string.Empty;

	/// <summary>
	///   Gets or initializes the display name.
	/// </summary>
	/// <value>
	///   The display name.
	/// </value>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	///   Gets or initializes the email address.
	/// </summary>
	/// <value>
	///   The email address.
	/// </value>
	public string Email { get; init; } = string.Empty;

	/// <summary>
	///   Create an Empty UserDto instance for default values
	/// </summary>
	public static UserDto Empty => new() { Id = string.Empty, Name = string.Empty, Email = string.Empty };
}
