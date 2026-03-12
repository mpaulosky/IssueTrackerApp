// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     PaginatedResponse.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   PaginatedResponse record for paginated API responses.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
[Serializable]
public record PaginatedResponse<T>(
	IReadOnlyList<T> Items,
	long Total,
	int Page,
	int PageSize)
{
	/// <summary>
	///   Gets the total number of pages.
	/// </summary>
	public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

	public static PaginatedResponse<T> Empty =>
		new(Array.Empty<T>(), 0, 1, 10);
}
