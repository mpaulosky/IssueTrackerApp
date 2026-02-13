// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =============================================

namespace Shared.Models.DTOs;

/// <summary>
///   CategoryDto record for simplified category representation
/// </summary>
[Serializable]
public record CategoryDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> record.
	/// </summary>
	public CategoryDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> record.
	/// </summary>
	/// <param name="category">The category.</param>
	public CategoryDto(Category category)
	{
		Id = category.Id;
		CategoryName = category.CategoryName;
		CategoryDescription = category.CategoryDescription;
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> record.
	/// </summary>
	/// <param name="categoryName">Name of the category.</param>
	/// <param name="categoryDescription">The category description.</param>
	public CategoryDto(string categoryName, string categoryDescription) : this()
	{
		CategoryName = categoryName;
		CategoryDescription = categoryDescription;
	}

	/// <summary>
	///   Gets or initializes the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	public ObjectId Id { get; init; } = ObjectId.Empty;

	/// <summary>
	///   Gets or initializes the name of the category.
	/// </summary>
	/// <value>
	///   The name of the category.
	/// </value>
	public string CategoryName { get; init; } = string.Empty;

	/// <summary>
	///   Gets or initializes the category description.
	/// </summary>
	/// <value>
	///   The category description.
	/// </value>
	public string CategoryDescription { get; init; } = string.Empty;

	/// <summary>
	///   Create an Empty CategoryDto instance for default values
	/// </summary>
	public static CategoryDto Empty => new() { Id = ObjectId.Empty, CategoryName = string.Empty, CategoryDescription = string.Empty };
}
