// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IUserService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Services;

public interface IUserService
{
	Task ArchiveUser(UserModel user);

	Task CreateUser(UserModel user);

	Task<UserModel> GetUser(string? userId);

	Task<UserModel> GetUserFromAuthentication(string? userObjectIdentifierId);

	Task<List<UserModel>> GetUsers();

	Task UpdateUser(UserModel user);
}
