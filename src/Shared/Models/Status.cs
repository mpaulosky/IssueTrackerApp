// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Status.cs
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
///   Status class
/// </summary>
[Serializable]
public class Status : Entity
{

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

}
