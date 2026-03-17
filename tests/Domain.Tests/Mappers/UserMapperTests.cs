// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UserMapperTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Mappers;

/// <summary>
///   Unit tests for UserMapper static mapping methods.
/// </summary>
public sealed class UserMapperTests
{
	#region ToDto(User) Tests

	[Fact]
	public void ToDto_FromUser_WithValidUser_ReturnsCorrectDto()
	{
		// Arrange
		var user = new User
		{
			Id = "auth0|123456",
			Name = "John Doe",
			Email = "john.doe@example.com"
		};

		// Act
		var result = UserMapper.ToDto(user);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be("auth0|123456");
		result.Name.Should().Be("John Doe");
		result.Email.Should().Be("john.doe@example.com");
	}

	[Fact]
	public void ToDto_FromUser_WithNullUser_ReturnsEmptyDto()
	{
		// Arrange
		User? user = null;

		// Act
		var result = UserMapper.ToDto(user);

		// Assert
		result.Should().Be(UserDto.Empty);
		result.Id.Should().BeEmpty();
		result.Name.Should().BeEmpty();
		result.Email.Should().BeEmpty();
	}

	[Fact]
	public void ToDto_FromUser_WithEmptyValues_ReturnsEmptyValuesDto()
	{
		// Arrange
		var user = new User
		{
			Id = string.Empty,
			Name = string.Empty,
			Email = string.Empty
		};

		// Act
		var result = UserMapper.ToDto(user);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().BeEmpty();
		result.Name.Should().BeEmpty();
		result.Email.Should().BeEmpty();
	}

	#endregion

	#region ToDto(UserInfo) Tests

	[Fact]
	public void ToDto_FromUserInfo_WithValidUserInfo_ReturnsCorrectDto()
	{
		// Arrange
		var userInfo = new UserInfo
		{
			Id = "auth0|654321",
			Name = "Jane Smith",
			Email = "jane.smith@example.com"
		};

		// Act
		var result = UserMapper.ToDto(userInfo);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be("auth0|654321");
		result.Name.Should().Be("Jane Smith");
		result.Email.Should().Be("jane.smith@example.com");
	}

	[Fact]
	public void ToDto_FromUserInfo_WithNullUserInfo_ReturnsEmptyDto()
	{
		// Arrange
		UserInfo? userInfo = null;

		// Act
		var result = UserMapper.ToDto(userInfo);

		// Assert
		result.Should().Be(UserDto.Empty);
	}

	[Fact]
	public void ToDto_FromUserInfo_WithEmptyUserInfo_ReturnsEmptyValuesDto()
	{
		// Arrange
		var userInfo = UserInfo.Empty;

		// Act
		var result = UserMapper.ToDto(userInfo);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().BeEmpty();
		result.Name.Should().BeEmpty();
		result.Email.Should().BeEmpty();
	}

	#endregion

	#region ToModel Tests

	[Fact]
	public void ToModel_WithValidDto_ReturnsCorrectModel()
	{
		// Arrange
		var dto = new UserDto("auth0|789012", "Bob Wilson", "bob.wilson@example.com");

		// Act
		var result = UserMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be("auth0|789012");
		result.Name.Should().Be("Bob Wilson");
		result.Email.Should().Be("bob.wilson@example.com");
	}

	[Fact]
	public void ToModel_WithNullDto_ReturnsEmptyModel()
	{
		// Arrange
		UserDto? dto = null;

		// Act
		var result = UserMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().BeEmpty();
		result.Name.Should().BeEmpty();
		result.Email.Should().BeEmpty();
	}

	[Fact]
	public void ToModel_WithEmptyDto_ReturnsEmptyModel()
	{
		// Arrange
		var dto = UserDto.Empty;

		// Act
		var result = UserMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().BeEmpty();
		result.Name.Should().BeEmpty();
		result.Email.Should().BeEmpty();
	}

	#endregion

	#region ToInfo Tests

	[Fact]
	public void ToInfo_WithValidDto_ReturnsCorrectUserInfo()
	{
		// Arrange
		var dto = new UserDto("auth0|111222", "Alice Johnson", "alice@example.com");

		// Act
		var result = UserMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be("auth0|111222");
		result.Name.Should().Be("Alice Johnson");
		result.Email.Should().Be("alice@example.com");
	}

	[Fact]
	public void ToInfo_WithNullDto_ReturnsEmptyUserInfo()
	{
		// Arrange
		UserDto? dto = null;

		// Act
		var result = UserMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().BeEmpty();
		result.Name.Should().BeEmpty();
		result.Email.Should().BeEmpty();
	}

	[Fact]
	public void ToInfo_WithEmptyDto_ReturnsEmptyValuesUserInfo()
	{
		// Arrange
		var dto = UserDto.Empty;

		// Act
		var result = UserMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().BeEmpty();
		result.Name.Should().BeEmpty();
		result.Email.Should().BeEmpty();
	}

	#endregion

	#region ToDtoList Tests

	[Fact]
	public void ToDtoList_WithValidUsers_ReturnsCorrectDtoList()
	{
		// Arrange
		var users = new List<User>
		{
			new() { Id = "user-1", Name = "User One", Email = "user1@example.com" },
			new() { Id = "user-2", Name = "User Two", Email = "user2@example.com" },
			new() { Id = "user-3", Name = "User Three", Email = "user3@example.com" }
		};

		// Act
		var result = UserMapper.ToDtoList(users);

		// Assert
		result.Should().NotBeNull();
		result.Should().HaveCount(3);
		result[0].Id.Should().Be("user-1");
		result[1].Id.Should().Be("user-2");
		result[2].Id.Should().Be("user-3");
	}

	[Fact]
	public void ToDtoList_WithNullCollection_ReturnsEmptyList()
	{
		// Arrange
		IEnumerable<User>? users = null;

		// Act
		var result = UserMapper.ToDtoList(users);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_WithEmptyCollection_ReturnsEmptyList()
	{
		// Arrange
		var users = new List<User>();

		// Act
		var result = UserMapper.ToDtoList(users);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_WithSingleUser_ReturnsSingleElementList()
	{
		// Arrange
		var users = new List<User>
		{
			new() { Id = "only-user", Name = "Only User", Email = "only@example.com" }
		};

		// Act
		var result = UserMapper.ToDtoList(users);

		// Assert
		result.Should().HaveCount(1);
		result[0].Name.Should().Be("Only User");
	}

	#endregion

	#region Round-Trip Tests

	[Fact]
	public void RoundTrip_UserToModelToDto_PreservesData()
	{
		// Arrange
		var originalDto = new UserDto("auth0|roundtrip", "Round Trip User", "roundtrip@example.com");

		// Act
		var model = UserMapper.ToModel(originalDto);
		var resultDto = UserMapper.ToDto(model);

		// Assert
		resultDto.Id.Should().Be(originalDto.Id);
		resultDto.Name.Should().Be(originalDto.Name);
		resultDto.Email.Should().Be(originalDto.Email);
	}

	[Fact]
	public void RoundTrip_UserInfoToDtoToInfo_PreservesData()
	{
		// Arrange
		var originalInfo = new UserInfo
		{
			Id = "auth0|infotrip",
			Name = "Info Trip User",
			Email = "infotrip@example.com"
		};

		// Act
		var dto = UserMapper.ToDto(originalInfo);
		var resultInfo = UserMapper.ToInfo(dto);

		// Assert
		resultInfo.Id.Should().Be(originalInfo.Id);
		resultInfo.Name.Should().Be(originalInfo.Name);
		resultInfo.Email.Should().Be(originalInfo.Email);
	}

	#endregion
}
