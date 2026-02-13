// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     StatusModel.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shared.Models;

/// <summary>
///   StatusModel class
/// </summary>
[Serializable]
public class Status
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
	///   Gets or sets the name of the status.
	/// </summary>
	/// <value>
	///   The name of the status.
	/// </value>
	[BsonElement("status_name")]
	[BsonRepresentation(BsonType.String)]
	public string StatusName { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the status description.
	/// </summary>
	/// <value>
	///   The status description.
	/// </value>
	[BsonElement("status_description")]
	[BsonRepresentation(BsonType.String)]
	public string StatusDescription { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets a value indicating whether this <see cref="Status" /> is archived.
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
}
