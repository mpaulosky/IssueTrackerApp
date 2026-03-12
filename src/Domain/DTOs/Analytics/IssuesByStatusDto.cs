namespace Domain.DTOs.Analytics;

/// <summary>
/// Represents the count of issues grouped by status.
/// </summary>
[Serializable]
[method: JsonConstructor]
public record IssuesByStatusDto(
	[property: JsonPropertyName("status")] string Status,
	[property: JsonPropertyName("count")] int Count);
