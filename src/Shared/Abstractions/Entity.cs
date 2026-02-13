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
	///   Gets the unique identifier for this entity.
	/// </summary>
	/// <value>
	///   The unique identifier.
	/// </value>
	[Key]
	public ObjectId Id { get; protected init; } = ObjectId.GenerateNewId();

	/// <summary>
	///   Gets the date and time when this entity was created.
	/// </summary>
	/// <value>
	///   The date and time when this entity was created in UTC.
	/// </value>
	[Required(ErrorMessage = "A Created On Date is required")]
	[BsonRepresentation(BsonType.DateTime)]
	[Display(Name = "Created On")]
	public DateTime CreatedOn { get; protected init; } = DateTime.UtcNow;

	/// <summary>
	///   Gets or sets the date and time when this entity was last modified.
	/// </summary>
	/// <value>
	///   The date and time when this entity was last modified, or <see langword="null" /> if never modified.
	/// </value>
	[BsonElement("modifiedOn")]
	[BsonRepresentation(BsonType.DateTime)]
	[Display(Name = "Modified On")]
	public DateTime? ModifiedOn { get; set; }

	/// <summary>
	///   Gets or sets a value indicating whether this entity is archived.
	/// </summary>
	/// <value>
	///   <see langword="true" /> if archived; otherwise, <see langword="false" />. The default is <see langword="false" />.
	/// </value>
	[BsonElement("archived")]
	[Display(Name = "Archived")]
	public bool Archived { get; set; }

	/// <summary>
	///   Gets or sets the user who archived this entity.
	/// </summary>
	/// <value>
	///   The user who archived this entity, or an empty <see cref="UserDto" /> if not archived.
	/// </value>
	[BsonElement("archivedBy")]
	[Display(Name = "Archived By")]
	public UserDto ArchivedBy { get; set; } = UserDto.Empty;

}
