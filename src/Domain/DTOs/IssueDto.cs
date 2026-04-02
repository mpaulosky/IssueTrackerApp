// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   IssueDto record
/// </summary>
[Serializable]
[method: JsonConstructor]
public record IssueDto(
	ObjectId Id,
	string Title,
	string Description,
	DateTime DateCreated,
	DateTime? DateModified,
	UserDto Author,
	CategoryDto Category,
	StatusDto Status,
	bool Archived,
	UserDto ArchivedBy,
	bool ApprovedForRelease,
	bool Rejected,
	UserDto Assignee,
	int Votes,
	IReadOnlyList<string> VotedBy,
	IReadOnlyList<string> Labels)
{
	/// <summary>
	///   Initializes a new instance of the <see cref="IssueDto" /> record.
	/// </summary>
	/// <param name="issue">The issue.</param>
	public IssueDto(Issue issue) : this(
		issue.Id,
		issue.Title,
		issue.Description,
		issue.DateCreated,
		issue.DateModified,
		UserMapper.ToDto(issue.Author),
		CategoryMapper.ToDto(issue.Category),
		StatusMapper.ToDto(issue.Status),
		issue.Archived,
		UserMapper.ToDto(issue.ArchivedBy),
		issue.ApprovedForRelease,
		issue.Rejected,
		UserMapper.ToDto(issue.Assignee),
		issue.Votes ?? 0,
		issue.VotedBy ?? [],
		issue.Labels ?? [])
	{
	}

	public static IssueDto Empty => new(
		ObjectId.Empty,
		string.Empty,
		string.Empty,
		DateTime.UtcNow,
		null,
		UserDto.Empty,
		CategoryDto.Empty,
		StatusDto.Empty,
		false,
		UserDto.Empty,
		false,
		false,
		UserDto.Empty,
		0,
		[],
		[]);
}
