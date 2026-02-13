// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     ICommentService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

using Shared.Models;
using Shared.Models.DTOs;

namespace Shared.Interfaces.Services;

public interface ICommentService
{
	Task ArchiveComment(Comment comment);

	Task CreateComment(Comment comment);

	Task<Comment> GetComment(string commentId);

	Task<List<Comment>> GetComments();

	Task<List<Comment>> GetCommentsByUser(string userId);

	Task<List<Comment>> GetCommentsByIssue(IssueDto issue);

	Task UpdateComment(Comment comment);

	Task UpVoteComment(string commentId, string userId);
}
