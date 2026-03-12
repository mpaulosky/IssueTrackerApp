// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Category.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

/// <summary>
///   Category class
/// </summary>
[Serializable]
public class Category
{
	/// <summary>
	///   Gets or sets the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	[BsonId]
	[BsonElement("_id")]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the name of the category.
	/// </summary>
	/// <value>
	///   The name of the category.
	/// </value>
	[BsonElement("category_name")]
	[BsonRepresentation(BsonType.String)]
	public string CategoryName { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the category description.
	/// </summary>
	/// <value>
	///   The category description.
	/// </value>
	[BsonElement("category-description")]
	[BsonRepresentation(BsonType.String)]
	public string CategoryDescription { get; set; } = string.Empty;

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
	///   Gets or sets a value indicating whether this <see cref="Category" /> is archived.
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
	public UserDto ArchivedBy { get; set; } = UserDto.Empty;
}
