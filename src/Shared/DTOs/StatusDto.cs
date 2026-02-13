// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     StatusDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

namespace Shared.Models.DTOs;

/// <summary>
///   StatusDto class
/// </summary>
[Serializable]
public class StatusDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="StatusDto" /> class.
	/// </summary>
	public StatusDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="StatusDto" /> class.
	/// </summary>
	/// <param name="status">The status.</param>
	public StatusDto(Status status)
	{
		StatusName = status.StatusName;
		StatusDescription = status.StatusDescription;
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="StatusDto" /> class.
	/// </summary>
	/// <param name="statusName">Name of the status.</param>
	/// <param name="statusDescription">The status description.</param>
	public StatusDto(string statusName, string statusDescription) : this()
	{
		StatusName = statusName;
		StatusDescription = statusDescription;
	}

	/// <summary>
	///   Gets the name of the status.
	/// </summary>
	/// <value>
	///   The name of the status.
	/// </value>
	public string StatusName { get; init; } = string.Empty;

	/// <summary>
	///   Gets the status description.
	/// </summary>
	/// <value>
	///   The status description.
	/// </value>
	public string StatusDescription { get; init; } = string.Empty;

}

