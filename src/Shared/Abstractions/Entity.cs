// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Entity.cs
// Company :       mpaulosky
// Author :        Matthew
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =======================================================

using System.ComponentModel.DataAnnotations;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using Shared.Models.DTOs;

namespace Shared.Abstractions;

/// <summary>
///   Base class for all entities in the domain model.
/// </summary>
public abstract class Entity
{

	/// <summary>
	///   /// Gets the unique identifier for this entity.
	/// </summary>
	[Key]
	public ObjectId Id { get; protected init; } = ObjectId.GenerateNewId();

	/// <summary>
	///   Gets the date and time when this entity was created.
	/// </summary>
	[Required(ErrorMessage = "A Created On Date is required")]
	[BsonRepresentation(BsonType.DateTime)]
	[Display(Name = "Created On")]
	public DateTime CreatedOn { get; protected init; } = DateTime.UtcNow;

	/// <summary>
	///   Gets or sets the date and time when this entity was last modified.
	/// </summary>
	[BsonElement("modifiedOn")]
	[BsonRepresentation(BsonType.DateTime)]
	[Display(Name = "Modified On")]
	public DateTime? ModifiedOn { get; set; }

	/// <summary>
	///   Gets or sets the archived status of the entity.
	/// </summary>
	[BsonElement("archived")]
	[Display(Name = "Archived")]
	public bool Archived { get; set; }

	[BsonElement("archivedBy")]
	[Display(Name = "Archived By")]
	public UserDto ArchivedBy { get; set; } = UserDto.Empty;

}
