// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =============================================

namespace Shared.Models.DTOs;

/// <summary>
///   CommentDto record
/// </summary>
[Serializable]
public record CommentDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="CommentDto" /> record.
	/// </summary>
	public CommentDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="CommentDto" /> record.
	/// </summary>
	/// <param name="comment">The comment.</param>
	public CommentDto(Comment comment)
	{
		Id = comment.Id;
		Title = comment.Title;
		Description = comment.Description;
		DateCreated = comment.CreatedOn;
		Issue = comment.Issue!;
		Author = comment.Author;
	}

	/// <summary>
	///   Gets or initializes the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	public ObjectId Id { get; init; } = ObjectId.Empty;

	/// <summary>
	///   Gets or initializes the title.
	/// </summary>
	/// <value>
	///   The title.
	/// </value>
	public string Title { get; init; } = string.Empty;

	/// <summary>
	///   Gets or initializes the description.
	/// </summary>
	/// <value>
	///   The description.
	/// </value>
	public string Description { get; init; } = string.Empty;

	/// <summary>
	///   Gets or initializes the date created.
	/// </summary>
	/// <value>
	///   The date created.
	/// </value>
	public DateTime DateCreated { get; init; } = DateTime.UtcNow;

	/// <summary>
	///   Gets or initializes the issue.
	/// </summary>
	/// <value>
	///   The issue.
	/// </value>
	public IssueDto Issue { get; init; } = new();

	/// <summary>
	///   Gets or initializes the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public UserDto Author { get; init; } = new();

	/// <summary>
	///   Create an Empty CommentDto instance for default values
	/// </summary>
	public static CommentDto Empty => new() { Id = ObjectId.Empty, Title = string.Empty, Description = string.Empty, DateCreated = DateTime.UtcNow, Issue = IssueDto.Empty, Author = UserDto.Empty };
}
