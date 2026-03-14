// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserMapper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Mappers;

/// <summary>
///   Static mapper for User, UserDto, and UserInfo conversions.
/// </summary>
public static class UserMapper
{
	/// <summary>
	///   Converts a User model to a UserDto.
	/// </summary>
	/// <param name="user">The user model.</param>
	/// <returns>A UserDto instance.</returns>
	public static UserDto ToDto(User? user)
	{
		if (user is null) { return UserDto.Empty; }

		return new UserDto(user.Id, user.Name, user.Email);
	}

	/// <summary>
	///   Converts a UserInfo value object to a UserDto.
	/// </summary>
	/// <param name="info">The user info value object.</param>
	/// <returns>A UserDto instance.</returns>
	public static UserDto ToDto(UserInfo? info)
	{
		if (info is null) { return UserDto.Empty; }

		return new UserDto(info.Id, info.Name, info.Email);
	}

	/// <summary>
	///   Converts a UserDto to a User model.
	/// </summary>
	/// <param name="dto">The user DTO.</param>
	/// <returns>A User model instance.</returns>
	public static User ToModel(UserDto? dto)
	{
		if (dto is null) { return new User(); }

		return new User
		{
			Id = dto.Id,
			Name = dto.Name,
			Email = dto.Email
		};
	}

	/// <summary>
	///   Converts a UserDto to a UserInfo value object.
	/// </summary>
	/// <param name="dto">The user DTO.</param>
	/// <returns>A UserInfo instance.</returns>
	public static UserInfo ToInfo(UserDto? dto)
	{
		if (dto is null) { return UserInfo.Empty; }

		return new UserInfo
		{
			Id = dto.Id,
			Name = dto.Name,
			Email = dto.Email
		};
	}

	/// <summary>
	///   Converts a collection of User models to a list of UserDto instances.
	/// </summary>
	/// <param name="users">The user models.</param>
	/// <returns>A list of UserDto instances.</returns>
	public static List<UserDto> ToDtoList(IEnumerable<User>? users)
	{
		if (users is null) { return []; }

		return users.Select(u => ToDto(u)).ToList();
	}
}
