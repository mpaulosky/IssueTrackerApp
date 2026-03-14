// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryInfo.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Models;

/// <summary>
///   CategoryInfo value object - embedded sub-document for category references in MongoDB.
///   Replaces CategoryDto as the embedded type within domain models.
/// </summary>
[Serializable]
public sealed class CategoryInfo
{
	/// <summary>
	///   Gets or sets the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the name of the category.
	/// </summary>
	/// <value>
	///   The name of the category.
	/// </value>
	public string CategoryName { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the category description.
	/// </summary>
	/// <value>
	///   The category description.
	/// </value>
	public string CategoryDescription { get; set; } = string.Empty;

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
	///   Gets or sets a value indicating whether this category is archived.
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
	///   Gets an empty CategoryInfo instance.
	/// </summary>
	public static CategoryInfo Empty => new();
}
