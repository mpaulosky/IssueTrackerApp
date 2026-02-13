// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =============================================

namespace Shared.Models.DTOs;

/// <summary>
///   IssueDto record for simplified issue representation
/// </summary>
[Serializable]
public record IssueDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="IssueDto" /> record.
	/// </summary>
	public IssueDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="IssueDto" /> record.
	/// </summary>
	/// <param name="issue">The issue.</param>
	public IssueDto(Issue issue)
	{
		Id = issue.Id;
		Title = issue.Title;
		Description = issue.Description;
		Author = issue.Author;
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
	///   Gets or initializes the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public UserDto Author { get; init; } = new();

	/// <summary>
	///   Create an Empty IssueDto instance for default values
	/// </summary>
	public static IssueDto Empty => new() { Id = ObjectId.Empty, Title = string.Empty, Description = string.Empty, Author = UserDto.Empty };
}
