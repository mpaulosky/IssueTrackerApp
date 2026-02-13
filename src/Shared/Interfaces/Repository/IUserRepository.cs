// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IUserRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Repository;

public interface IUserRepository
{
	Task ArchiveAsync(UserModel user);

	Task CreateAsync(UserModel user);

	Task<UserModel> GetAsync(string itemId);

	Task<UserModel> GetFromAuthenticationAsync(string userObjectIdentifierId);

	Task<IEnumerable<UserModel>> GetAllAsync();

	Task UpdateAsync(string itemId, UserModel user);
}
