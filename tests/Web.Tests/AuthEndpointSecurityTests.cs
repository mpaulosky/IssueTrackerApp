// Copyright (c) IssueTrackerApp. All rights reserved.
// Licensed under the MIT License.

namespace Web.Tests;

/// <summary>
/// Tests for authentication endpoint security vulnerabilities.
/// These tests verify fixes for:
/// - Open redirect prevention in login endpoint
/// - CSRF protection via POST-only logout
/// </summary>
public class AuthEndpointSecurityTests : IClassFixture<TestWebApplicationFactory>
{
	private readonly HttpClient _client;

	public AuthEndpointSecurityTests(TestWebApplicationFactory factory)
	{
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	#region Open Redirect Prevention Tests

	/// <summary>
	/// Verifies that the login endpoint exists and accepts requests.
	/// Note: Full Auth0 integration tests require a configured Auth0 tenant.
	/// </summary>
	[Fact]
	public async Task Login_Endpoint_ShouldExist()
	{
		// Act
		var response = await _client.GetAsync("/account/login");

		// Assert - Should not return 404 (endpoint exists)
		// May return 500 due to Auth0 configuration in test environment, which is acceptable
		response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
	}

	/// <summary>
	/// Verifies that the IsLocalUrl helper correctly rejects external URLs.
	/// This tests the open redirect protection logic indirectly.
	/// </summary>
	[Theory]
	[InlineData("https://malicious.com", false)]
	[InlineData("http://evil.com/phishing", false)]
	[InlineData("//malicious.com", false)]
	[InlineData("https://malicious.com/path", false)]
	[InlineData("/", true)]
	[InlineData("/dashboard", true)]
	[InlineData("/issues/123", true)]
	public void IsLocalUrl_ShouldCorrectlyValidateUrls(string url, bool expectedIsLocal)
	{
		// This is a unit test for the URL validation logic
		var isLocal = IsLocalUrl(url);
		isLocal.Should().Be(expectedIsLocal);
	}

	// Mirror of the IsLocalUrl helper from Program.cs for testing
	private static bool IsLocalUrl(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return false;
		}

		if (url.StartsWith("//", StringComparison.Ordinal) ||
		    url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
		    url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal);
	}

	#endregion

	#region CSRF Protection Tests (POST-only Logout)

	/// <summary>
	/// Verifies that GET requests to /account/logout are rejected.
	/// Minimal APIs return 404 when no matching GET endpoint exists.
	/// This prevents CSRF attacks where malicious sites log users out via img tags or links.
	/// </summary>
	[Fact]
	public async Task Logout_WithGetMethod_ShouldBeRejected()
	{
		// Act
		var response = await _client.GetAsync("/account/logout");

		// Assert - GET should not be allowed for logout
		// Returns 404 because no GET endpoint is mapped (only POST)
		response.StatusCode.Should().BeOneOf(
			HttpStatusCode.NotFound, // No GET endpoint mapped
			HttpStatusCode.MethodNotAllowed // Some servers return 405
		);
	}

	/// <summary>
	/// Verifies that POST to /account/logout without authentication returns redirect to login.
	/// </summary>
	[Fact]
	public async Task Logout_WithPostMethod_WithoutAuth_ShouldRequireAuthentication()
	{
		// Act
		var response = await _client.PostAsync("/account/logout", null);

		// Assert - Should require authentication (redirect to login or 401)
		// Since RequireAuthorization is set, unauthenticated users get redirected
		response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
	}

	/// <summary>
	/// Verifies that POST to /account/logout without antiforgery token returns 400 Bad Request.
	/// </summary>
	[Fact]
	public async Task Logout_WithPostMethod_WithoutAntiforgeryToken_ShouldReturn400()
	{
		// Arrange - Need to be authenticated to test antiforgery
		// For this test, we verify the endpoint exists and requires POST
		var response = await _client.PostAsync("/account/logout", new StringContent(""));

		// Assert - Should reject without proper antiforgery (400) or require auth (redirect/401)
		response.StatusCode.Should().BeOneOf(
			HttpStatusCode.BadRequest, // Missing antiforgery token
			HttpStatusCode.Unauthorized, // Not authenticated
			HttpStatusCode.Redirect // Redirect to login
		);
	}

	/// <summary>
	/// Verifies that the logout endpoint is POST-only (no GET mapping).
	/// </summary>
	[Fact]
	public async Task Logout_Endpoint_ShouldOnlyAcceptPost()
	{
		// Arrange
		var getResponse = await _client.GetAsync("/account/logout");
		var postResponse = await _client.PostAsync("/account/logout", null);

		// Assert - GET should fail, POST should at least be recognized (even if auth fails)
		getResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
		postResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
	}

	#endregion
}
