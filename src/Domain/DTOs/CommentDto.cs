// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   CommentDto record
/// </summary>
[Serializable]
[method: JsonConstructor]
public record CommentDto(
	ObjectId Id,
	string Title,
	string Description,
	DateTime DateCreated,
	DateTime? DateModified,
	IssueDto Issue,
	UserDto Author,
	HashSet<string> UserVotes,
	bool Archived,
	UserDto ArchivedBy,
	bool IsAnswer,
	UserDto AnswerSelectedBy)
{
	/// <summary>
	///   Initializes a new instance of the <see cref="CommentDto" /> record.
	/// </summary>
	/// <param name="comment">The comment.</param>
	public CommentDto(Comment comment) : this(
		comment.Id,
		comment.Title,
		comment.Description,
		comment.DateCreated,
		comment.DateModified,
		comment.Issue,
		comment.Author,
		comment.UserVotes,
		comment.Archived,
		comment.ArchivedBy,
		comment.IsAnswer,
		comment.AnswerSelectedBy)
	{
	}

	public static CommentDto Empty => new(
		ObjectId.Empty,
		string.Empty,
		string.Empty,
		DateTime.UtcNow,
		null,
		IssueDto.Empty,
		UserDto.Empty,
		[],
		false,
		UserDto.Empty,
		false,
		UserDto.Empty);
}
