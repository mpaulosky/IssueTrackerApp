// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CreateIssueDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace Web.Models;

/// <summary>
///   Represents the data transfer object for creating a new issue.
/// </summary>
public class CreateIssueDto
{
	/// <summary>
	///   Gets or sets the title.
	/// </summary>
	/// <value>
	///   The title of the issue. The maximum length is 75 characters.
	/// </value>
	[Required]
	[MaxLength(75)]
	public string? Title { get; set; }

	/// <summary>
	///   Gets or sets the description.
	/// </summary>
	/// <value>
	///   The description of the issue. The maximum length is 500 characters.
	/// </value>
	[Required]
	[MaxLength(500)]
	public string? Description { get; set; }

	/// <summary>
	///   Gets or sets the category identifier.
	/// </summary>
	/// <value>
	///   The identifier of the category.
	/// </value>
	[Required]
	public string? CategoryId { get; set; }
}
