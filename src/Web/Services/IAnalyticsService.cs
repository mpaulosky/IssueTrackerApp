// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IAnalyticsService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.DTOs.Analytics;

namespace Web.Services;

/// <summary>
/// Service interface for analytics operations.
/// </summary>
public interface IAnalyticsService
{
	/// <summary>
	/// Gets comprehensive analytics summary for the dashboard.
	/// </summary>
	/// <param name="startDate">Optional start date for filtering.</param>
	/// <param name="endDate">Optional end date for filtering.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Analytics summary.</returns>
	Task<Result<AnalyticsSummaryDto>> GetAnalyticsSummaryAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets issue counts grouped by status.
	/// </summary>
	/// <param name="startDate">Optional start date for filtering.</param>
	/// <param name="endDate">Optional end date for filtering.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of issues by status.</returns>
	Task<Result<IReadOnlyList<IssuesByStatusDto>>> GetIssuesByStatusAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets issue counts grouped by category.
	/// </summary>
	/// <param name="startDate">Optional start date for filtering.</param>
	/// <param name="endDate">Optional end date for filtering.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of issues by category.</returns>
	Task<Result<IReadOnlyList<IssuesByCategoryDto>>> GetIssuesByCategoryAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets issue creation and closure counts over time.
	/// </summary>
	/// <param name="startDate">Optional start date for filtering.</param>
	/// <param name="endDate">Optional end date for filtering.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of issues over time.</returns>
	Task<Result<IReadOnlyList<IssuesOverTimeDto>>> GetIssuesOverTimeAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets average resolution times grouped by category.
	/// </summary>
	/// <param name="startDate">Optional start date for filtering.</param>
	/// <param name="endDate">Optional end date for filtering.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of resolution times by category.</returns>
	Task<Result<IReadOnlyList<ResolutionTimeDto>>> GetResolutionTimesAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets top contributors based on closed issues and comment counts.
	/// </summary>
	/// <param name="startDate">Optional start date for filtering.</param>
	/// <param name="endDate">Optional end date for filtering.</param>
	/// <param name="topCount">Number of top contributors to return.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of top contributors.</returns>
	Task<Result<IReadOnlyList<TopContributorDto>>> GetTopContributorsAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		int topCount = 10,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Exports analytics data as CSV.
	/// </summary>
	/// <param name="startDate">Optional start date for filtering.</param>
	/// <param name="endDate">Optional end date for filtering.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>CSV data as byte array.</returns>
	Task<Result<byte[]>> ExportAnalyticsAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken cancellationToken = default);
}
