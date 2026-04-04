// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ServiceDefaultsEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests verifying that the <c>/health</c> and <c>/alive</c> endpoints
///   registered by <c>ServiceDefaults.MapDefaultEndpoints()</c> respond correctly.
/// </summary>
[Collection("Integration")]
public sealed class ServiceDefaultsEndpointTests : IntegrationTestBase
{
	public ServiceDefaultsEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	// -----------------------------------------------------------------------
	// /health
	// -----------------------------------------------------------------------

	/// <summary>
	///   The /health endpoint aggregates all registered health checks.
	///   When the application is running normally it must return 200 OK with
	///   a plain-text body of "Healthy".
	/// </summary>
	[Fact]
	public async Task HealthEndpoint_Returns200_WhenAppIsHealthy()
	{
		// Arrange – health endpoints are public; no auth header is required.
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/health");

		// Assert
		// Health endpoints must be publicly accessible (no authentication required).
		// A 401 here is a hard failure: Aspire/Kubernetes probes cannot send tokens,
		// so an auth-protected health endpoint would break liveness/readiness checks.
		response.StatusCode.Should().Be(
			HttpStatusCode.OK,
			"health endpoint must be publicly accessible – Aspire and Kubernetes probes do not send auth tokens");

		var body = await response.Content.ReadAsStringAsync();
		body.Should().Be("Healthy", because: "all default health checks should pass in the test environment");
	}

	// -----------------------------------------------------------------------
	// /alive
	// -----------------------------------------------------------------------

	/// <summary>
	///   The /alive endpoint evaluates only checks tagged "live".
	///   When the process is running it must return 200 OK so that orchestrators
	///   (Kubernetes, Aspire dashboard) know the container is alive.
	/// </summary>
	[Fact]
	public async Task AliveEndpoint_Returns200_WhenAppIsRunning()
	{
		// Arrange – alive probes are also public by Aspire convention.
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/alive");

		// Assert
		// Alive endpoints must be publicly accessible for the same reasons as /health.
		// A 401 is a hard failure: orchestrators cannot authenticate their probes.
		response.StatusCode.Should().Be(
			HttpStatusCode.OK,
			"alive endpoint must be publicly accessible – Aspire and Kubernetes probes do not send auth tokens");

		var body = await response.Content.ReadAsStringAsync();
		body.Should().Be("Healthy", because: "the application process should be live in the test environment");
	}
}
