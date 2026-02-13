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
	Task ArchiveUser(Shared.Models.User user);

	Task CreateUser(Shared.Models.User user);

	Task<Shared.Models.User> GetUser(string? userId);

	Task<Shared.Models.User> GetUserFromAuthentication(string? userObjectIdentifierId);

	Task<List<Shared.Models.User>> GetUsers();

	Task UpdateUser(Shared.Models.User user);
}
