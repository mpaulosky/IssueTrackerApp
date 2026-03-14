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
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the title.
	/// </summary>
	/// <value>
	///   The title.
	/// </value>
	[BsonElement("issue_title")]
	[BsonRepresentation(BsonType.String)]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the description.
	/// </summary>
	/// <value>
	///   The description.
	/// </value>
	[BsonElement("issue_description")]
	[BsonRepresentation(BsonType.String)]
	public string Description { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the date created.
	/// </summary>
	/// <value>
	///   The date created.
	/// </value>
	[BsonElement("date_created")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime DateCreated { get; init; } = DateTime.UtcNow;

	/// <summary>
	///   Gets or sets the date modified.
	/// </summary>
	/// <value>
	///   The date modified.
	/// </value>
	[BsonElement("date_modified")]
	[BsonRepresentation(BsonType.DateTime)]
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
	[BsonElement("archived")]
	[BsonRepresentation(BsonType.Boolean)]
	public bool Archived { get; set; }

	/// <summary>
	///   Gets or sets who archived the record.
	/// </summary>
	/// <value>
	///   Who archived the record.
	/// </value>
	[BsonElement("archived_by")]
	public UserInfo ArchivedBy { get; set; } = UserInfo.Empty;

	/// <summary>
	///   Gets or sets a value indicating whether [approved for release].
	/// </summary>
	/// <value>
	///   <c>true</c> if [approved for release]; otherwise, <c>false</c>.
	/// </value>
	[BsonElement("approved_for_release")]
	[BsonRepresentation(BsonType.Boolean)]
	public bool ApprovedForRelease { get; set; }

	/// <summary>
	///   Gets or sets a value indicating whether this <see cref="Issue" /> is rejected.
	/// </summary>
	/// <value>
	///   <c>true</c> if rejected; otherwise, <c>false</c>.
	/// </value>
	[BsonElement("rejected")]
	[BsonRepresentation(BsonType.Boolean)]
	public bool Rejected { get; set; }
}
