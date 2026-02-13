// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =============================================

namespace Shared.Models.DTOs;

/// <summary>
///   StatusDto record for simplified status representation
/// </summary>
[Serializable]
public record StatusDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="StatusDto" /> record.
	/// </summary>
	public StatusDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="StatusDto" /> record.
	/// </summary>
	/// <param name="status">The status.</param>
	public StatusDto(Status status)
	{
		Id = status.Id;
		StatusName = status.StatusName;
		StatusDescription = status.StatusDescription;
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="StatusDto" /> record.
	/// </summary>
	/// <param name="statusName">Name of the status.</param>
	/// <param name="statusDescription">The status description.</param>
	public StatusDto(string statusName, string statusDescription) : this()
	{
		StatusName = statusName;
		StatusDescription = statusDescription;
	}

	/// <summary>
	///   Gets or initializes the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	public ObjectId Id { get; init; } = ObjectId.Empty;

	/// <summary>
	///   Gets or initializes the name of the status.
	/// </summary>
	/// <value>
	///   The name of the status.
	/// </value>
	public string StatusName { get; init; } = string.Empty;

	/// <summary>
	///   Gets or initializes the status description.
	/// </summary>
	/// <value>
	///   The status description.
	/// </value>
	public string StatusDescription { get; init; } = string.Empty;

	/// <summary>
	///   Create an Empty StatusDto instance for default values
	/// </summary>
	public static StatusDto Empty => new() { Id = ObjectId.Empty, StatusName = string.Empty, StatusDescription = string.Empty };
}

