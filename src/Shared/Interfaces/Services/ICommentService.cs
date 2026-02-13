// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     ICommentService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.Services
// =============================================

namespace Shared.Interfaces.Services;

public interface ICommentService
{
	Task ArchiveComment(CommentModel comment);

	Task CreateComment(CommentModel comment);

	Task<CommentModel> GetComment(string commentId);

	Task<List<CommentModel>> GetComments();

	Task<List<CommentModel>> GetCommentsByUser(string userId);

	Task<List<CommentModel>> GetCommentsByIssue(BasicIssueModel issue);

	Task UpdateComment(CommentModel comment);

	Task UpVoteComment(string commentId, string userId);
}
