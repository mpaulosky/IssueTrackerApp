// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueSearchRequest.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   Request object for searching and filtering issues.
/// </summary>
[Serializable]
public record IssueSearchRequest
{
	/// <summary>
	///   Gets the search text to filter issues by title or description.
	/// </summary>
	public string? SearchText { get; init; }

	/// <summary>
	///   Gets the status name to filter by.
	/// </summary>
	public string? StatusFilter { get; init; }

	/// <summary>
	///   Gets the category name to filter by.
	/// </summary>
	public string? CategoryFilter { get; init; }

	/// <summary>
	///   Gets the author ID to filter by.
	/// </summary>
	public string? AuthorId { get; init; }

	/// <summary>
	///   Gets the start date for filtering issues by creation date.
	/// </summary>
	public DateOnly? DateFrom { get; init; }

	/// <summary>
	///   Gets the end date for filtering issues by creation date.
	/// </summary>
	public DateOnly? DateTo { get; init; }

	/// <summary>
	///   Gets the page number for pagination. Default is 1.
	/// </summary>
	public int Page { get; init; } = 1;

	/// <summary>
	///   Gets the page size for pagination. Default is 20.
	/// </summary>
	public int PageSize { get; init; } = 20;

	/// <summary>
	///   Gets a value indicating whether to include archived issues.
	/// </summary>
	public bool IncludeArchived { get; init; }

	/// <summary>
	///   Gets an empty search request with default values.
	/// </summary>
	public static IssueSearchRequest Empty => new();

	/// <summary>
	///   Returns true if any filters are active.
	/// </summary>
	public bool HasActiveFilters =>
		!string.IsNullOrWhiteSpace(SearchText) ||
		!string.IsNullOrWhiteSpace(StatusFilter) ||
		!string.IsNullOrWhiteSpace(CategoryFilter) ||
		!string.IsNullOrWhiteSpace(AuthorId) ||
		DateFrom.HasValue ||
		DateTo.HasValue;
}
