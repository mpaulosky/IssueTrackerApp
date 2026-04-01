// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LabelEndpoints.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.Features.Issues.Queries;

using MediatR;

namespace Web.Endpoints;

/// <summary>
///   Extension methods for mapping Label API endpoints.
/// </summary>
public static class LabelEndpoints
{
	/// <summary>
	///   Maps Label API endpoints to the route builder.
	/// </summary>
	public static IEndpointRouteBuilder MapLabelEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/labels")
			.WithTags("Labels");

		group.MapGet("/suggestions", GetLabelSuggestions)
			.WithName("GetLabelSuggestions")
			.WithDescription("Gets label suggestions based on prefix")
			.RequireAuthorization()
			.Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest);

		return app;
	}

	/// <summary>
	///   Gets label suggestions based on prefix.
	/// </summary>
	private static async Task<IResult> GetLabelSuggestions(
		IMediator mediator,
		string prefix,
		int max = 10,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(prefix))
		{
			return Results.BadRequest(new { error = "Prefix cannot be empty" });
		}

		var query = new GetLabelSuggestionsQuery(prefix, max);
		var result = await mediator.Send(query, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode == ResultErrorCode.Validation
				? Results.BadRequest(new { error = result.Error })
				: Results.Problem(result.Error ?? "Failed to retrieve label suggestions");
		}

		return Results.Ok(result.Value);
	}
}
