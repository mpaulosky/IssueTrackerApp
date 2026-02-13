// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     ICommentRepository.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Models;
using Shared.Models.DTOs;

namespace Shared.Interfaces.Repository;

public interface ICommentRepository
{
	Task ArchiveAsync(Comment comment);

	Task CreateAsync(Comment comment);

	Task<Comment> GetAsync(string itemId);

	Task<IEnumerable<Comment>?> GetAllAsync();

	Task<IEnumerable<Comment>> GetByUserAsync(string userId);

	Task<IEnumerable<Comment>> GetByIssueAsync(IssueDto issue);

	Task UpdateAsync(string itemId, Comment comment);

	Task UpVoteAsync(string itemId, string userId);
}
