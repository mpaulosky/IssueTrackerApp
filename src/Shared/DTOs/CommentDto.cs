// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CommentDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

namespace Shared.Models.DTOs;

/// <summary>
///   CommentDto class
/// </summary>
[Serializable]
public class CommentDto
{

	/// <summary>
	///   Initializes a new instance of the <see cref="CommentDto" /> class.
	/// </summary>
	/// <param name="comment">The comment.</param>
	public CommentDto(Comment comment)
	{
		Id = comment.Id;
		Title = comment.Title;
		Description = comment.Description;
		DateCreated = comment.DateCreated;
		Issue = comment.Issue!;
		Author = comment.Author;
	}

	public string Id { get; init; }
	public string Title { get; init; }
	public string Description { get; init; }
	public DateTime DateCreated { get; init; }
	public BasicIssueModel Issue { get; init; }
	public BasicUserModel Author { get; init; }
}
