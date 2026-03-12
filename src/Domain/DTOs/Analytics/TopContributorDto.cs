namespace Domain.DTOs.Analytics;

/// <summary>
/// Represents top contributors with their issue and comment statistics.
/// </summary>
[Serializable]
[method: JsonConstructor]
public record TopContributorDto(
	[property: JsonPropertyName("userId")] string UserId,
	[property: JsonPropertyName("userName")] string UserName,
	[property: JsonPropertyName("issuesClosed")] int IssuesClosed,
	[property: JsonPropertyName("commentsCount")] int CommentsCount);
