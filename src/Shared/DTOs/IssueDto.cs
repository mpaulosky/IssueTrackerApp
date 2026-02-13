// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IssueDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

namespace Shared.Models.DTOs;

/// <summary>
///   IssueDto class
/// </summary>
[Serializable]
public class IssueDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="IssueDto" /> class.
	/// </summary>
	public IssueDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="IssueDto" /> class.
	/// </summary>
	/// <param name="issue">The issue.</param>
	public IssueDto(Issue issue)
	{
		Id = issue.Id;
		Title = issue.Title;
		Description = issue.Description;
		DateCreated = issue.DateCreated;
		Category = new CategoryDto(issue.Category);
		Status = new StatusDto(issue.IssueStatus);
		Author = new UserDto(issue.Author);
	}

	/// <summary>
	///   Gets or sets the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	public string Id { get; init; } = string.Empty;

	/// <summary>
	///   Gets or sets the title.
	/// </summary>
	/// <value>
	///   The title.
	/// </value>
	public string Title { get; init; } = string.Empty;

	/// <summary>
	///   Gets or sets the description.
	/// </summary>
	/// <value>
	///   The description.
	/// </value>
	public string Description { get; init; } = string.Empty;

	/// <summary>
	///   Gets or sets the date created.
	/// </summary>
	/// <value>
	///   The date created.
	/// </value>
	public DateTime DateCreated { get; init; } = DateTime.UtcNow;


	/// <summary>
	///   Gets or sets the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public UserDto Author { get; set; } = new();

	/// <summary>
	///   Gets or sets the category.
	/// </summary>
	/// <value>
	///   The category.
	/// </value>
	public CategoryDto Category { get; init; } = new();

	/// <summary>
	///   Gets or sets the status.
	/// </summary>
	/// <value>
	///   The status.
	/// </value>
	public StatusDto Status { get; init; } = new();
}
