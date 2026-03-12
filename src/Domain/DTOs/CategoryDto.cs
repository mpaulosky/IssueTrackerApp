// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   CategoryDto record
/// </summary>
[Serializable]
[method: JsonConstructor]
public record CategoryDto(
	ObjectId Id,
	string CategoryName,
	string CategoryDescription,
	DateTime DateCreated,
	DateTime? DateModified,
	bool Archived,
	UserDto ArchivedBy)
{
	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> record.
	/// </summary>
	/// <param name="category">The category.</param>
	public CategoryDto(Category category) : this(
		category.Id,
		category.CategoryName,
		category.CategoryDescription,
		category.DateCreated,
		category.DateModified,
		category.Archived,
		category.ArchivedBy)
	{
	}

	public static CategoryDto Empty => new(
			ObjectId.Empty,
			string.Empty,
			string.Empty,
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
}
