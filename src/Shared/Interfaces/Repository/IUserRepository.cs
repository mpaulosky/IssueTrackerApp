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
	Task ArchiveAsync(Shared.Models.User user);

	Task CreateAsync(Shared.Models.User user);

	Task<Shared.Models.User> GetAsync(string itemId);

	Task<Shared.Models.User> GetFromAuthenticationAsync(string userObjectIdentifierId);

	Task<IEnumerable<Shared.Models.User>> GetAllAsync();

	Task UpdateAsync(string itemId, Shared.Models.User user);
}
