// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     BasicCategoryModel.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

namespace Shared.Models.DTOs;

/// <summary>
///   Basic Category Model class
/// </summary>
[Serializable]
public class CategoryDto
{
	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> class.
	/// </summary>
	public CategoryDto()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> class.
	/// </summary>
	/// <param name="category">The category.</param>
	public CategoryDto(Category category)
	{
		CategoryName = category.CategoryName;
		CategoryDescription = category.CategoryDescription;
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="CategoryDto" /> class.
	/// </summary>
	/// <param name="categoryName">Name of the category.</param>
	/// <param name="categoryDescription">The category description.</param>
	public CategoryDto(string categoryName, string categoryDescription) : this()
	{
		CategoryName = categoryName;
		CategoryDescription = categoryDescription;
	}

	/// <summary>
	///   Gets the name of the category.
	/// </summary>
	/// <value>
	///   The name of the category.
	/// </value>
	public string CategoryName { get; init; } = string.Empty;

	/// <summary>
	///   Gets the category description.
	/// </summary>
	/// <value>
	///   The category description.
	/// </value>
	public string CategoryDescription { get; init; } = string.Empty;
}
