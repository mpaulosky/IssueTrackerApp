namespace Domain.DTOs.Analytics;

/// <summary>
/// Represents average resolution time for issues grouped by category.
/// </summary>
[Serializable]
[method: JsonConstructor]
public record ResolutionTimeDto(
	[property: JsonPropertyName("category")] string Category,
	[property: JsonPropertyName("averageHours")] double AverageHours);
