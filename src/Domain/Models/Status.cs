// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Status.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

/// <summary>
///   Status class
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
	public ObjectId Id { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the name of the status.
	/// </summary>
	/// <value>
	///   The name of the status.
	/// </value>
	public string StatusName { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the status description.
	/// </summary>
	/// <value>
	///   The status description.
	/// </value>
	public string StatusDescription { get; set; } = string.Empty;

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
	///   Gets or sets a value indicating whether this <see cref="Status" /> is archived.
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
}
