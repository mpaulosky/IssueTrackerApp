// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     VoteEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Domain.DTOs;
using Domain.Models;

using MongoDB.Bson;

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests for Vote API endpoints.
///   Tests POST /api/issues/{id}/vote  (cast vote)
///   and  DELETE /api/issues/{id}/vote (remove vote).
/// </summary>
[Collection("Integration")]
public sealed class VoteEndpointTests : IntegrationTestBase
{
	public VoteEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	// -----------------------------------------------------------------
	// Private helpers
	// -----------------------------------------------------------------

	/// <summary>
	/// Seeds an issue whose VotedBy list already contains the given userId,
	/// so that remove-vote tests have a pre-voted issue to work with.
	/// </summary>
	private async Task<Issue> SeedIssueWithVoteAsync(
		Category category,
		Status status,
		string userId)
	{
		var issue = await SeedIssueAsync(category, status);

		await using var ctx = CreateDbContext();
		var stored = await ctx.Issues.FindAsync(issue.Id);
		stored!.VotedBy = [userId];
		stored.Votes = 1;
		await ctx.SaveChangesAsync();

		// Return a refreshed copy with the mutated state
		issue.VotedBy = [userId];
		issue.Votes = 1;
		return issue;
	}

	// -----------------------------------------------------------------
	// POST /api/issues/{id}/vote  —  Cast Vote
	// -----------------------------------------------------------------

	#region POST /api/issues/{id}/vote - Cast Vote

	[Fact]
	public async Task CastVote_ReturnsOk_WhenAuthenticatedAndIssueExists()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/vote", content: null);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var dto = await response.Content.ReadFromJsonAsync<IssueDto>(JsonOptions);
		dto.Should().NotBeNull();
		dto!.Votes.Should().Be(1);
		dto.VotedBy.Should().Contain(TestAuthHandler.TestUserId);
	}

	[Fact]
	public async Task CastVote_ReturnsUnauthorized_WhenUnauthenticated()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/vote", content: null);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task CastVote_ReturnsForbidden_WhenAuthenticatedWithoutUserRole()
	{
		// Arrange — Admin role does not satisfy "UserPolicy" (which requires Role = "User")
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/vote", content: null);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task CastVote_ReturnsNotFound_WhenIssueDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.PostAsync($"/api/issues/{nonExistentId}/vote", content: null);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CastVote_ReturnsBadRequest_WhenUserAlreadyVoted()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();

		// First vote — should succeed
		var firstResponse = await client.PostAsync($"/api/issues/{issue.Id}/vote", content: null);
		firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		// Act — second vote by the same user
		var response = await client.PostAsync($"/api/issues/{issue.Id}/vote", content: null);

		// Assert — duplicate vote rejected
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	#endregion

	// -----------------------------------------------------------------
	// DELETE /api/issues/{id}/vote  —  Remove Vote
	// -----------------------------------------------------------------

	#region DELETE /api/issues/{id}/vote - Remove Vote

	[Fact]
	public async Task RemoveVote_ReturnsOk_WhenAuthenticatedAndHasVoted()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueWithVoteAsync(categories[0], statuses[0], TestAuthHandler.TestUserId);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/issues/{issue.Id}/vote");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var dto = await response.Content.ReadFromJsonAsync<IssueDto>(JsonOptions);
		dto.Should().NotBeNull();
		dto!.Votes.Should().Be(0);
		dto.VotedBy.Should().NotContain(TestAuthHandler.TestUserId);
	}

	[Fact]
	public async Task RemoveVote_ReturnsUnauthorized_WhenUnauthenticated()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.DeleteAsync($"/api/issues/{issue.Id}/vote");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task RemoveVote_ReturnsForbidden_WhenAuthenticatedWithoutUserRole()
	{
		// Arrange — Admin role does not satisfy "UserPolicy"
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.DeleteAsync($"/api/issues/{issue.Id}/vote");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task RemoveVote_ReturnsNotFound_WhenIssueDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/issues/{nonExistentId}/vote");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task RemoveVote_ReturnsBadRequest_WhenUserHasNotVoted()
	{
		// Arrange — issue exists but user has NOT voted on it
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/issues/{issue.Id}/vote");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	#endregion

	// -----------------------------------------------------------------
	// Round-trip: vote then unvote restores original state
	// -----------------------------------------------------------------

	#region Round-trip

	[Fact]
	public async Task CastAndRemoveVote_RestoresOriginalVoteCount()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();

		// Act — cast then remove
		var castResponse = await client.PostAsync($"/api/issues/{issue.Id}/vote", content: null);
		castResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var removeResponse = await client.DeleteAsync($"/api/issues/{issue.Id}/vote");
		removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var dto = await removeResponse.Content.ReadFromJsonAsync<IssueDto>(JsonOptions);

		// Assert — back to zero
		dto.Should().NotBeNull();
		dto!.Votes.Should().Be(0);
		dto.VotedBy.Should().BeEmpty();
	}

	#endregion
}
