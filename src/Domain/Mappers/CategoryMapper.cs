// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryMapper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Mappers;

/// <summary>
///   Static mapper for Category, CategoryDto, and CategoryInfo conversions.
/// </summary>
public static class CategoryMapper
{
	/// <summary>
	///   Converts a Category model to a CategoryDto.
	/// </summary>
	/// <param name="category">The category model.</param>
	/// <returns>A CategoryDto instance.</returns>
	public static CategoryDto ToDto(Category? category)
	{
		if (category is null) { return CategoryDto.Empty; }

		return new CategoryDto(
			category.Id,
			category.CategoryName,
			category.CategoryDescription,
			category.DateCreated,
			category.DateModified,
			category.Archived,
			UserMapper.ToDto(category.ArchivedBy));
	}

	/// <summary>
	///   Converts a CategoryInfo value object to a CategoryDto.
	/// </summary>
	/// <param name="info">The category info value object.</param>
	/// <returns>A CategoryDto instance.</returns>
	public static CategoryDto ToDto(CategoryInfo? info)
	{
		if (info is null) { return CategoryDto.Empty; }

		return new CategoryDto(
			info.Id,
			info.CategoryName,
			info.CategoryDescription,
			info.DateCreated,
			info.DateModified,
			info.Archived,
			UserMapper.ToDto(info.ArchivedBy));
	}

	/// <summary>
	///   Converts a CategoryDto to a Category model.
	/// </summary>
	/// <param name="dto">The category DTO.</param>
	/// <returns>A Category model instance.</returns>
	public static Category ToModel(CategoryDto? dto)
	{
		if (dto is null) { return new Category(); }

		return new Category
		{
			Id = dto.Id,
			CategoryName = dto.CategoryName,
			CategoryDescription = dto.CategoryDescription,
			DateCreated = dto.DateCreated,
			DateModified = dto.DateModified,
			Archived = dto.Archived,
			ArchivedBy = UserMapper.ToInfo(dto.ArchivedBy)
		};
	}

	/// <summary>
	///   Converts a CategoryDto to a CategoryInfo value object.
	/// </summary>
	/// <param name="dto">The category DTO.</param>
	/// <returns>A CategoryInfo instance.</returns>
	public static CategoryInfo ToInfo(CategoryDto? dto)
	{
		if (dto is null) { return CategoryInfo.Empty; }

		return new CategoryInfo
		{
			Id = dto.Id,
			CategoryName = dto.CategoryName,
			CategoryDescription = dto.CategoryDescription,
			DateCreated = dto.DateCreated,
			DateModified = dto.DateModified,
			Archived = dto.Archived,
			ArchivedBy = UserMapper.ToInfo(dto.ArchivedBy)
		};
	}

	/// <summary>
	///   Converts a collection of Category models to a list of CategoryDto instances.
	/// </summary>
	/// <param name="categories">The category models.</param>
	/// <returns>A list of CategoryDto instances.</returns>
	public static List<CategoryDto> ToDtoList(IEnumerable<Category>? categories)
	{
		if (categories is null) { return []; }

		return categories.Select(c => ToDto(c)).ToList();
	}
}
