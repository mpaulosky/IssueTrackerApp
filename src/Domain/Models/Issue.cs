// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Issue.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

/// <summary>
///   Issue class
/// </summary>
[Serializable]
public class Issue
{
	/// <summary>
	///   Gets or sets the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	[BsonId]
	public ObjectId Id { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the title.
	/// </summary>
	/// <value>
	///   The title.
	/// </value>
	public string Title { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the description.
	/// </summary>
	/// <value>
	///   The description.
	/// </value>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the date created.
	/// </summary>
	/// <value>
	///   The date created.
	/// </value>
	public DateTime DateCreated { get; init; } = DateTime.UtcNow;

	/// <summary>
	///   Gets or sets the date modified.
	/// </summary>
	/// <value>
	///   The date modified.
	/// </value>
	public DateTime? DateModified { get; set; }

	/// <summary>
	///   Gets or sets the category.
	/// </summary>
	/// <value>
	///   The category.
	/// </value>
	public CategoryInfo Category { get; set; } = CategoryInfo.Empty;

	/// <summary>
	///   Gets or sets the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public UserInfo Author { get; set; } = UserInfo.Empty;

	/// <summary>
	///   Gets or sets the issue status.
	/// </summary>
	/// <value>
	///   The issue status.
	/// </value>
	public StatusInfo Status { get; set; } = StatusInfo.Empty;

	/// <summary>
	///   Gets or sets a value indicating whether this <see cref="Issue" /> is archived.
	/// </summary>
	/// <value>
	///   <c>true</c> if archived; otherwise, <c>false</c>.
	/// </value>
	public bool Archived { get; set; }

	/// <summary>
	///   Gets or sets who archived the record.
	/// </summary>
	/// <value>
	///   Who archived the record.
	/// </value>
	public UserInfo ArchivedBy { get; set; } = UserInfo.Empty;

	/// <summary>
	///   Gets or sets the user assigned to this issue.
	/// </summary>
	/// <value>
	///   The assigned user, or <see cref="UserInfo.Empty" /> if unassigned.
	/// </value>
	public UserInfo Assignee { get; set; } = UserInfo.Empty;

	/// <summary>
	///   Gets or sets a value indicating whether [approved for release].
	/// </summary>
	/// <value>
	///   <c>true</c> if [approved for release]; otherwise, <c>false</c>.
	/// </value>
	public bool ApprovedForRelease { get; set; }

	/// <summary>
	///   Gets or sets a value indicating whether this <see cref="Issue" /> is rejected.
	/// </summary>
	/// <value>
	///   <c>true</c> if rejected; otherwise, <c>false</c>.
	/// </value>
	public bool Rejected { get; set; }

	/// <summary>
	///   Gets or sets the number of votes this issue has received.
	/// </summary>
	/// <value>
	///   The vote count.
	/// </value>
	public int Votes { get; set; } = 0;

	/// <summary>
	///   Gets or sets the list of user IDs that have voted for this issue.
	/// </summary>
	/// <value>
	///   The collection of voter user IDs.
	/// </value>
	public List<string>? VotedBy { get; set; } = [];

	/// <summary>
	///   Gets or sets the labels assigned to this issue.
	/// </summary>
	/// <value>
	///   The collection of label strings.
	/// </value>
	public List<string>? Labels { get; set; } = [];
}
