// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CategoryModel.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using Shared.Abstractions;

namespace Shared.Models;

/// <summary>
///   CategoryModel class
/// </summary>
[Serializable]
public class Category : Entity
{

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

}
