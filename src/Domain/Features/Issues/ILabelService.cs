// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ILabelService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Features.Issues;

/// <summary>
///   Service for label suggestions and management.
/// </summary>
public interface ILabelService
{
	/// <summary>
	///   Gets label suggestions based on prefix.
	/// </summary>
	/// <param name="prefix">The prefix to search for (case-insensitive).</param>
	/// <param name="maxResults">Maximum number of suggestions to return (default 10).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A list of label suggestions ordered by frequency then alphabetically.</returns>
	Task<IReadOnlyList<string>> GetSuggestionsAsync(
		string prefix,
		int maxResults = 10,
		CancellationToken cancellationToken = default);
}
