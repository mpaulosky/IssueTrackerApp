// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Issue.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using Shared.Abstractions;
using Shared.Models.DTOs;

namespace Shared.Models;

/// <summary>
///   Issue class
/// </summary>
[Serializable]
public class Issue : Entity
{

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
	///   Gets or sets the category.
	/// </summary>
	/// <value>
	///   The category.
	/// </value>
	public CategoryDto Category { get; set; } = CategoryDto.Empty;

	/// <summary>
	///   Gets or sets the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public UserDto Author { get; set; } = UserDto.Empty;

	/// <summary>
	///   Gets or sets the issue status.
	/// </summary>
	/// <value>
	///   The issue status.
	/// </value>
	public StatusDto IssueStatus { get; set; } = StatusDto.Empty;

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
