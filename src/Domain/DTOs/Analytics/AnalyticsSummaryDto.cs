namespace Domain.DTOs.Analytics;

/// <summary>
/// Comprehensive analytics summary containing all dashboard metrics.
/// </summary>
[Serializable]
[method: JsonConstructor]
public record AnalyticsSummaryDto(
	[property: JsonPropertyName("totalIssues")] int TotalIssues,
	[property: JsonPropertyName("openIssues")] int OpenIssues,
	[property: JsonPropertyName("closedIssues")] int ClosedIssues,
	[property: JsonPropertyName("averageResolutionHours")] double AverageResolutionHours,
	[property: JsonPropertyName("byStatus")] IReadOnlyList<IssuesByStatusDto> ByStatus,
	[property: JsonPropertyName("byCategory")] IReadOnlyList<IssuesByCategoryDto> ByCategory,
	[property: JsonPropertyName("overTime")] IReadOnlyList<IssuesOverTimeDto> OverTime,
	[property: JsonPropertyName("topContributors")] IReadOnlyList<TopContributorDto> TopContributors);
