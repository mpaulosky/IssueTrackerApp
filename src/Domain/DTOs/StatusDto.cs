// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   StatusDto record
/// </summary>
[Serializable]
[method: JsonConstructor]
public record StatusDto(
	ObjectId Id,
	string StatusName,
	string StatusDescription,
	DateTime DateCreated,
	DateTime? DateModified,
	bool Archived,
	UserDto ArchivedBy)
{
	/// <summary>
	///   Initializes a new instance of the <see cref="StatusDto" /> record.
	/// </summary>
	/// <param name="status">The status.</param>
	public StatusDto(Status status) : this(
		status.Id,
		status.StatusName,
		status.StatusDescription,
		status.DateCreated,
		status.DateModified,
		status.Archived,
		UserMapper.ToDto(status.ArchivedBy))
	{
	}

	public static StatusDto Empty => new(
			ObjectId.Empty,
			string.Empty,
			string.Empty,
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
}
