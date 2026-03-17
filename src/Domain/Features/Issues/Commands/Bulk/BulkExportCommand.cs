// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkExportCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using System.Text;

using Domain.Abstractions;

namespace Domain.Features.Issues.Commands.Bulk;

/// <summary>
///   Command to export multiple issues to CSV format.
/// </summary>
public record BulkExportCommand(
	List<string> IssueIds,
	string RequestedBy) : IRequest<Result<BulkExportResult>>;

/// <summary>
///   Result of a bulk export operation.
/// </summary>
/// <param name="CsvContent">The CSV content as bytes.</param>
/// <param name="FileName">The suggested filename for the export.</param>
/// <param name="TotalExported">Number of issues exported.</param>
/// <param name="Errors">List of errors for issues that couldn't be exported.</param>
public record BulkExportResult(
	byte[] CsvContent,
	string FileName,
	int TotalExported,
	List<BulkOperationError> Errors);

/// <summary>
///   Handler for bulk export operations.
/// </summary>
public sealed class BulkExportCommandHandler : IRequestHandler<BulkExportCommand, Result<BulkExportResult>>
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<BulkExportCommandHandler> _logger;

	public BulkExportCommandHandler(
		IRepository<Issue> repository,
		ILogger<BulkExportCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<Result<BulkExportResult>> Handle(
		BulkExportCommand request,
		CancellationToken cancellationToken)
	{
		if (request.IssueIds.Count == 0)
		{
			return Result.Fail<BulkExportResult>("No issues specified for export.");
		}

		if (request.IssueIds.Count > BulkOperationConstants.MaxBatchSize)
		{
			return Result.Fail<BulkExportResult>(
				$"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize} items.");
		}

		_logger.LogInformation(
			"Processing bulk export for {Count} issues",
			request.IssueIds.Count);

		var errors = new List<BulkOperationError>();
		var issues = new List<Issue>();

		foreach (var issueId in request.IssueIds)
		{
			try
			{
				var existingResult = await _repository.GetByIdAsync(issueId, cancellationToken);

				if (existingResult.Failure || existingResult.Value is null)
				{
					errors.Add(new BulkOperationError(issueId, "Issue not found"));
					continue;
				}

				issues.Add(existingResult.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching issue {IssueId} for export", issueId);
				errors.Add(new BulkOperationError(issueId, ex.Message));
			}
		}

		var csv = GenerateCsv(issues);
		var fileName = $"issues_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

		_logger.LogInformation(
			"Bulk export completed: {Exported} exported, {Failed} failed",
			issues.Count,
			errors.Count);

		return Result.Ok(new BulkExportResult(
			csv,
			fileName,
			issues.Count,
			errors));
	}

	private static byte[] GenerateCsv(List<Issue> issues)
	{
		var sb = new StringBuilder();

		// CSV Header
		sb.AppendLine("Id,Title,Description,Status,Category,Author,DateCreated,DateModified,Archived");

		foreach (var issue in issues)
		{
			sb.AppendLine(string.Join(",",
				EscapeCsvField(issue.Id.ToString()),
				EscapeCsvField(issue.Title),
				EscapeCsvField(issue.Description),
				EscapeCsvField(issue.Status.StatusName),
				EscapeCsvField(issue.Category.CategoryName),
				EscapeCsvField(issue.Author.Name),
				issue.DateCreated.ToString("yyyy-MM-dd HH:mm:ss"),
				issue.DateModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
				issue.Archived.ToString()
			));
		}

		return Encoding.UTF8.GetBytes(sb.ToString());
	}

	private static string EscapeCsvField(string field)
	{
		if (string.IsNullOrEmpty(field))
		{
			return "\"\"";
		}

		// Escape quotes and wrap in quotes if contains special characters
		if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
		{
			return $"\"{field.Replace("\"", "\"\"")}\"";
		}

		return $"\"{field}\"";
	}
}
