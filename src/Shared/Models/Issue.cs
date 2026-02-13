// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     IssueModel.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using Shared.Models.DTOs;

namespace Shared.Models;

/// <summary>
///   IssueModel class
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
	public string Id { get; set; } = string.Empty;

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
	///   Gets or sets the category.
	/// </summary>
	/// <value>
	///   The category.
	/// </value>
	public BasicCategoryModel Category { get; set; } = new();

	/// <summary>
	///   Gets or sets the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public BasicUserModel Author { get; set; } = new();

	/// <summary>
	///   Gets or sets the issue status.
	/// </summary>
	/// <value>
	///   The issue status.
	/// </value>
	public BasicStatusModel IssueStatus { get; set; } = new();

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
	public BasicUserModel ArchivedBy { get; set; } = new();

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
