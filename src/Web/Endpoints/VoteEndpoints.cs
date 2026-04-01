// Copyright (c) 2026. All rights reserved.

using Domain.Abstractions;
using Domain.DTOs;

using Microsoft.AspNetCore.SignalR;

using Web.Hubs;
using Web.Services;

namespace Web.Endpoints;

/// <summary>
///   Extension methods for mapping Vote API endpoints.
/// </summary>
public static class VoteEndpoints
{
	/// <summary>
	///   Maps Vote API endpoints to the route builder.
	/// </summary>
	public static IEndpointRouteBuilder MapVoteEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/issues/{id}/vote")
			.WithTags("Votes");

		group.MapPost("/", CastVote)
			.WithName("CastVote")
			.WithDescription("Casts a vote for the current user on the specified issue")
			.RequireAuthorization("UserPolicy")
			.Produces<IssueDto>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest);

		group.MapDelete("/", RemoveVote)
			.WithName("RemoveVote")
			.WithDescription("Removes the current user's vote from the specified issue")
			.RequireAuthorization("UserPolicy")
			.Produces<IssueDto>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest);

		return app;
	}

	/// <summary>
	///   Casts a vote for the current user on the specified issue.
	/// </summary>
	private static async Task<IResult> CastVote(
		string id,
		IVotingService votingService,
		IHubContext<IssueHub> hubContext,
		CancellationToken cancellationToken = default)
	{
		var result = await votingService.VoteAsync(id, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode switch
			{
				ResultErrorCode.NotFound => Results.NotFound(new { error = result.Error }),
				ResultErrorCode.Validation => Results.BadRequest(new { error = result.Error }),
				_ => Results.Problem(result.Error ?? "Failed to cast vote")
			};
		}

		await hubContext.Clients.All.SendAsync("IssueVoted", result.Value, cancellationToken);

		return Results.Ok(result.Value);
	}

	/// <summary>
	///   Removes the current user's vote from the specified issue.
	/// </summary>
	private static async Task<IResult> RemoveVote(
		string id,
		IVotingService votingService,
		IHubContext<IssueHub> hubContext,
		CancellationToken cancellationToken = default)
	{
		var result = await votingService.UnvoteAsync(id, cancellationToken);

		if (result.Failure)
		{
			return result.ErrorCode switch
			{
				ResultErrorCode.NotFound => Results.NotFound(new { error = result.Error }),
				ResultErrorCode.Validation => Results.BadRequest(new { error = result.Error }),
				_ => Results.Problem(result.Error ?? "Failed to remove vote")
			};
		}

		await hubContext.Clients.All.SendAsync("IssueVoted", result.Value, cancellationToken);

		return Results.Ok(result.Value);
	}
}
