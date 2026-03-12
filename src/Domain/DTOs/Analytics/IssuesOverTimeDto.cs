namespace Domain.DTOs.Analytics;

/// <summary>
/// Represents issue creation and closure counts over time.
/// </summary>
[Serializable]
[method: JsonConstructor]
public record IssuesOverTimeDto(
	[property: JsonPropertyName("date")] DateTime Date,
	[property: JsonPropertyName("created")] int Created,
	[property: JsonPropertyName("closed")] int Closed);
