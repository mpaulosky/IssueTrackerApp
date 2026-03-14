// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UserDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   UserDto record
/// </summary>
[Serializable]
[method: JsonConstructor]
public record UserDto(string Id, string Name, string Email)
{
	/// <summary>
	///   Initializes a new instance of the <see cref="UserDto" /> record.
	/// </summary>
	/// <param name="user">The user.</param>
	public UserDto(User user) : this(user.Id, user.Name, user.Email)
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="UserDto" /> record from a UserInfo value object.
	/// </summary>
	/// <param name="info">The user info value object.</param>
	public UserDto(UserInfo info) : this(info.Id, info.Name, info.Email)
	{
	}

	public static UserDto Empty => new(string.Empty, string.Empty, string.Empty);
}
