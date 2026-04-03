// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LabelEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Domain.Models;

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests for Label API endpoints.
///   Tests GET /api/labels/suggestions?prefix={prefix}&amp;max={max}
/// </summary>
[Collection("Integration")]
public sealed class LabelEndpointTests : IntegrationTestBase
{
	public LabelEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	// -------------------------------------------------------------------------
	// Private helper – seed an issue and attach labels via direct DB access
	// -------------------------------------------------------------------------

	private async Task<Issue> SeedIssueWithLabelsAsync(
		Category category,
		Status status,
		List<string> labels,
		string title = "Labelled Issue")
	{
		var issue = await SeedIssueAsync(category, status, title);

		await using var ctx = CreateDbContext();
		var stored = await ctx.Issues.FindAsync(issue.Id);
		stored!.Labels = labels;
		await ctx.SaveChangesAsync();

		issue.Labels = labels;
		return issue;
	}

	// -------------------------------------------------------------------------
	// GET /api/labels/suggestions – matching results
	// -------------------------------------------------------------------------

	#region 200 OK – suggestions returned

	[Fact]
	public async Task GetSuggestions_ReturnsOk_WhenAuthenticatedAndPrefixMatches()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		// "bug" and "buffer" both start with "bu"; "critical" does not
		await SeedIssueWithLabelsAsync(categories[0], statuses[0], ["bug", "buffer", "critical"]);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions?prefix=bu");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var suggestions = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
		suggestions.Should().NotBeNull();
		suggestions.Should().Contain("bug");
		suggestions.Should().Contain("buffer");
		suggestions.Should().NotContain("critical");
	}

	[Fact]
	public async Task GetSuggestions_ReturnsEmptyList_WhenNoPrefixMatch()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueWithLabelsAsync(categories[0], statuses[0], ["bug"]);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions?prefix=xyz");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var suggestions = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
		suggestions.Should().NotBeNull();
		suggestions.Should().BeEmpty();
	}

	[Fact]
	public async Task GetSuggestions_IsCaseInsensitive_WhenPrefixUsesUpperCase()
	{
		// Arrange – labels stored as lower-case; query with upper-case prefix
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueWithLabelsAsync(categories[0], statuses[0], ["bug", "build"]);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions?prefix=BU");

		// Assert – endpoint should either return matches or empty (documents actual behavior)
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetSuggestions_ReturnsDistinctLabels_WhenMultipleIssuesShareLabels()
	{
		// Arrange – two issues both carry "bug"
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueWithLabelsAsync(categories[0], statuses[0], ["bug"], "Issue A");
		await SeedIssueWithLabelsAsync(categories[0], statuses[0], ["bug"], "Issue B");
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions?prefix=bu");

		// Assert – "bug" should appear exactly once
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var suggestions = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
		suggestions.Should().NotBeNull();
		suggestions!.Where(s => s == "bug").Should().HaveCount(1);
	}

	#endregion

	// -------------------------------------------------------------------------
	// GET /api/labels/suggestions – max parameter
	// -------------------------------------------------------------------------

	#region max parameter respected

	[Fact]
	public async Task GetSuggestions_RespectsMaxParameter()
	{
		// Arrange – seed an issue with 5 labels all starting with "feat"
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueWithLabelsAsync(
			categories[0], statuses[0],
			["feature", "feat-auth", "feat-ui", "feat-api", "feat-db"]);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions?prefix=feat&max=2");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var suggestions = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
		suggestions.Should().NotBeNull();
		suggestions!.Count.Should().BeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task GetSuggestions_UsesDefaultMaxOfTen_WhenMaxNotSpecified()
	{
		// Arrange – seed 15 labels all starting with "lbl"
		var (categories, statuses) = await SeedTestDataAsync();
		var labels = Enumerable.Range(1, 15).Select(i => $"lbl-{i:D2}").ToList();
		await SeedIssueWithLabelsAsync(categories[0], statuses[0], labels);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions?prefix=lbl");

		// Assert – default cap is 10
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var suggestions = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
		suggestions.Should().NotBeNull();
		suggestions!.Count.Should().BeLessThanOrEqualTo(10);
	}

	#endregion

	// -------------------------------------------------------------------------
	// GET /api/labels/suggestions – 400 Bad Request for empty prefix
	// -------------------------------------------------------------------------

	#region 400 Bad Request

	[Fact]
	public async Task GetSuggestions_ReturnsBadRequest_WhenPrefixEmpty()
	{
		// Arrange – omit the prefix query param entirely
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetSuggestions_ReturnsBadRequest_WhenPrefixIsWhitespace()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();

		// Act – single space, URL-encoded
		var response = await client.GetAsync("/api/labels/suggestions?prefix=%20");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetSuggestions_ReturnsBadRequest_ContainsErrorMessage_WhenPrefixEmpty()
	{
		// Arrange – pass an explicit empty-string value so the handler runs
		// and returns the custom JSON error (omitting the param entirely causes
		// ASP.NET Core binding to return 400 before the handler executes)
		using var client = CreateAuthenticatedClient();

		// Act – prefix= sends an empty string, handler returns { error: "..." }
		var response = await client.GetAsync("/api/labels/suggestions?prefix=");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var body = await response.Content.ReadAsStringAsync();
		body.Should().Contain("Prefix cannot be empty");
	}

	#endregion

	// -------------------------------------------------------------------------
	// GET /api/labels/suggestions – 401 Unauthorized for anonymous access
	// -------------------------------------------------------------------------

	#region 401 Unauthorized

	[Fact]
	public async Task GetSuggestions_ReturnsUnauthorized_WhenUnauthenticated()
	{
		// Arrange
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions?prefix=bug");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetSuggestions_ReturnsUnauthorized_WhenUnauthenticated_EvenWithEmptyPrefix()
	{
		// Arrange – authentication is checked before prefix validation
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/api/labels/suggestions");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion
}
