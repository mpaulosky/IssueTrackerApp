// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ExportAnalyticsQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;
using Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Domain.Features.Analytics.Queries;

/// <summary>
/// Query to export analytics data for CSV generation.
/// </summary>
public record ExportAnalyticsQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<Result<byte[]>>;

/// <summary>
/// Handler for ExportAnalyticsQuery that generates CSV data.
/// </summary>
public sealed class ExportAnalyticsQueryHandler
	: IRequestHandler<ExportAnalyticsQuery, Result<byte[]>>
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly ILogger<ExportAnalyticsQueryHandler> _logger;

	public ExportAnalyticsQueryHandler(
		IRepository<Issue> issueRepository,
		ILogger<ExportAnalyticsQueryHandler> logger)
	{
		_issueRepository = issueRepository;
		_logger = logger;
	}

	public async Task<Result<byte[]>> Handle(
		ExportAnalyticsQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Exporting analytics data from {StartDate} to {EndDate}",
				request.StartDate, request.EndDate);

			var startDate = request.StartDate ?? DateTime.MinValue;
			var endDate = request.EndDate ?? DateTime.MaxValue;

			var result = await _issueRepository.FindAsync(
				i => i.DateCreated >= startDate && i.DateCreated <= endDate,
				cancellationToken);

			if (result.Failure || result.Value is null)
			{
				_logger.LogWarning("Failed to retrieve issues for export");
				return Result.Fail<byte[]>(result.Error ?? "Failed to retrieve issues");
			}

			var issues = result.Value.ToList();

			// Build CSV content
			var csv = new StringBuilder();
			csv.AppendLine("ID,Title,Status,Category,Author,Created,Modified,ResolutionHours");

			foreach (var issue in issues)
			{
				var resolutionHours = issue.DateModified.HasValue
					? (issue.DateModified.Value - issue.DateCreated).TotalHours.ToString("F2")
					: "N/A";

				csv.AppendLine($"\"{issue.Id}\",\"{EscapeCsv(issue.Title)}\",\"{EscapeCsv(issue.Status.StatusName)}\"," +
					$"\"{EscapeCsv(issue.Category.CategoryName)}\",\"{EscapeCsv(issue.Author.Name)}\"," +
					$"\"{issue.DateCreated:yyyy-MM-dd HH:mm:ss}\",\"{issue.DateModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}\"," +
					$"\"{resolutionHours}\"");
			}

			var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
			_logger.LogInformation("Successfully exported {Count} issues to CSV", issues.Count);
			return Result.Ok(csvBytes);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error exporting analytics data");
			return Result.Fail<byte[]>($"Failed to export analytics data: {ex.Message}");
		}
	}

	private static string EscapeCsv(string value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;

		// Escape double quotes by doubling them
		return value.Replace("\"", "\"\"");
	}
}
