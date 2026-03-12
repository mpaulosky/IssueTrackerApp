// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     PagedResult.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   A generic paged result for API responses with pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
[Serializable]
public record PagedResult<T>
{
	/// <summary>
	///   Gets the items in the current page.
	/// </summary>
	public IReadOnlyList<T> Items { get; init; } = [];

	/// <summary>
	///   Gets the total count of items across all pages.
	/// </summary>
	public int TotalCount { get; init; }

	/// <summary>
	///   Gets the current page number (1-based).
	/// </summary>
	public int Page { get; init; }

	/// <summary>
	///   Gets the page size.
	/// </summary>
	public int PageSize { get; init; }

	/// <summary>
	///   Gets the total number of pages.
	/// </summary>
	public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

	/// <summary>
	///   Gets a value indicating whether there is a previous page.
	/// </summary>
	public bool HasPreviousPage => Page > 1;

	/// <summary>
	///   Gets a value indicating whether there is a next page.
	/// </summary>
	public bool HasNextPage => Page < TotalPages;

	/// <summary>
	///   Gets an empty paged result.
	/// </summary>
	public static PagedResult<T> Empty => new()
	{
		Items = [],
		TotalCount = 0,
		Page = 1,
		PageSize = 20
	};

	/// <summary>
	///   Creates a new paged result with the specified parameters.
	/// </summary>
	public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
	{
		return new PagedResult<T>
		{
			Items = items,
			TotalCount = totalCount,
			Page = page,
			PageSize = pageSize
		};
	}
}
