namespace Domain.DTOs.Analytics;

/// <summary>
/// Represents the count of issues grouped by category.
/// </summary>
[Serializable]
[method: JsonConstructor]
public record IssuesByCategoryDto(
	[property: JsonPropertyName("category")] string Category,
	[property: JsonPropertyName("count")] int Count);
