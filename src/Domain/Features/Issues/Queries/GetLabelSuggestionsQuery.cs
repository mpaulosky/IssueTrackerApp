// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetLabelSuggestionsQuery.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Issues.Queries;

/// <summary>
///   Query to get label suggestions based on prefix.
/// </summary>
public record GetLabelSuggestionsQuery(string Prefix, int MaxResults = 10) 
	: IRequest<Result<IReadOnlyList<string>>>;

/// <summary>
///   Handler for getting label suggestions.
/// </summary>
public sealed class GetLabelSuggestionsQueryHandler 
	: IRequestHandler<GetLabelSuggestionsQuery, Result<IReadOnlyList<string>>>
{
	private readonly ILabelService _labelService;
	private readonly ILogger<GetLabelSuggestionsQueryHandler> _logger;

	public GetLabelSuggestionsQueryHandler(
		ILabelService labelService,
		ILogger<GetLabelSuggestionsQueryHandler> logger)
	{
		_labelService = labelService;
		_logger = logger;
	}

	public async Task<Result<IReadOnlyList<string>>> Handle(
		GetLabelSuggestionsQuery request, 
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Prefix))
		{
			_logger.LogWarning("Label suggestions query received with empty prefix");
			return Result.Fail<IReadOnlyList<string>>("Prefix cannot be empty", ResultErrorCode.Validation);
		}

		_logger.LogInformation("Fetching label suggestions for prefix: {Prefix}", request.Prefix);

		var suggestions = await _labelService.GetSuggestionsAsync(
			request.Prefix, 
			request.MaxResults, 
			cancellationToken);

		_logger.LogInformation("Successfully fetched {Count} label suggestions", suggestions.Count);
		return Result.Ok(suggestions);
	}
}
