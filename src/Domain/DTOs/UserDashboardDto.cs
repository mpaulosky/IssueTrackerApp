// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UserDashboardDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   Data transfer object for user dashboard information.
/// </summary>
[Serializable]
public record UserDashboardDto(
	int TotalIssues,
	int OpenIssues,
	int ResolvedIssues,
	int ThisWeekIssues,
	IReadOnlyList<IssueDto> RecentIssues)
{
	/// <summary>
	///   Gets an empty dashboard instance.
	/// </summary>
	public static UserDashboardDto Empty => new(0, 0, 0, 0, []);
}
