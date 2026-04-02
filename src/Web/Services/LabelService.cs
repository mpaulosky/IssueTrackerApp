// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LabelService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.Features.Issues;
using Domain.Models;

namespace Web.Services;

/// <summary>
///   Service for label suggestions and management.
/// </summary>
public sealed class LabelService : ILabelService
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<LabelService> _logger;

	public LabelService(
		IRepository<Issue> repository,
		ILogger<LabelService> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	/// <summary>
	///   Gets label suggestions based on prefix.
	/// </summary>
	public async Task<IReadOnlyList<string>> GetSuggestionsAsync(
		string prefix,
		int maxResults = 10,
		CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Fetching label suggestions for prefix: {Prefix}", prefix);

		// Get all issues from repository
		var issuesResult = await _repository.GetAllAsync(cancellationToken);

		if (issuesResult.Failure || issuesResult.Value is null)
		{
			_logger.LogWarning("Failed to retrieve issues for label suggestions");
			return Array.Empty<string>();
		}

		// Extract all labels that start with the prefix (case-insensitive)
		var labelFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var issue in issuesResult.Value)
		{
			foreach (var label in issue.Labels ?? [])
			{
				if (label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					// Preserve original casing from first occurrence
					var existingKey = labelFrequency.Keys
						.FirstOrDefault(k => k.Equals(label, StringComparison.OrdinalIgnoreCase));

					if (existingKey is not null)
					{
						labelFrequency[existingKey]++;
					}
					else
					{
						labelFrequency[label] = 1;
					}
				}
			}
		}

		// Order by frequency descending, then alphabetically
		var suggestions = labelFrequency
			.OrderByDescending(kvp => kvp.Value)
			.ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
			.Take(maxResults)
			.Select(kvp => kvp.Key)
			.ToList();

		_logger.LogInformation("Found {Count} label suggestions for prefix: {Prefix}", 
			suggestions.Count, prefix);

		return suggestions.AsReadOnly();
	}
}
