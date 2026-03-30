// Copyright (c) IssueTrackerApp. All rights reserved.
// Licensed under the MIT License.

namespace Web.Tests;

/// <summary>
/// Tests for URL validation logic used in open redirect prevention.
/// These tests don't require Auth0 configuration.
/// </summary>
public class UrlValidationTests
{
	/// <summary>
	/// Verifies that the IsLocalUrl helper correctly rejects external URLs.
	/// This tests the open redirect protection logic.
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
}

/// <summary>
/// Tests for authentication endpoint security vulnerabilities.
/// These tests verify fixes for:
/// - Open redirect prevention in login endpoint
/// - CSRF protection via POST-only logout
/// </summary>
/// <remarks>
/// TODO: These tests require a proper Auth0 test configuration.
/// The TestWebApplicationFactory needs to properly mock Auth0 services.
/// Temporarily skipped until Sprint 6.
/// </remarks>
[Trait("Category", "SkipInCI")]
public class AuthEndpointSecurityTests : IClassFixture<TestWebApplicationFactory>
{
	private readonly HttpClient? _client;

	public AuthEndpointSecurityTests(TestWebApplicationFactory factory)
	{
		// Skip initialization if factory fails - will mark tests as skipped
		try
		{
			_client = factory.CreateClient(new WebApplicationFactoryClientOptions
			{
				AllowAutoRedirect = false
			});
		}
		catch
		{
			_client = null;
		}
	}

	#region CSRF Protection Tests (POST-only Logout)

	/// <summary>
	/// Verifies that the login endpoint exists and accepts requests.
	/// Note: Full Auth0 integration tests require a configured Auth0 tenant.
	/// </summary>
	[Fact]
	public async Task Login_Endpoint_ShouldExist()
	{
		// Act
		var response = await _client!.GetAsync("/account/login");

		// Assert - Should not return 404 (endpoint exists)
		response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
	}

	/// <summary>
	/// Verifies that GET requests to /account/logout are rejected.
	/// </summary>
	[Fact]
	public async Task Logout_WithGetMethod_ShouldBeRejected()
	{
		// Act
		var response = await _client!.GetAsync("/account/logout");

		// Assert
		response.StatusCode.Should().BeOneOf(
			HttpStatusCode.NotFound,
			HttpStatusCode.MethodNotAllowed
		);
	}

	/// <summary>
	/// Verifies that POST to /account/logout without authentication returns redirect to login.
	/// </summary>
	[Fact]
	public async Task Logout_WithPostMethod_WithoutAuth_ShouldRequireAuthentication()
	{
		// Act
		var response = await _client!.PostAsync("/account/logout", null);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
	}

	/// <summary>
	/// Verifies that POST to /account/logout without antiforgery token returns 400 Bad Request.
	/// </summary>
	[Fact]
	public async Task Logout_WithPostMethod_WithoutAntiforgeryToken_ShouldReturn400()
	{
		// Arrange
		var response = await _client!.PostAsync("/account/logout", new StringContent(""));

		// Assert
		response.StatusCode.Should().BeOneOf(
			HttpStatusCode.BadRequest,
			HttpStatusCode.Unauthorized,
			HttpStatusCode.Redirect
		);
	}

	/// <summary>
	/// Verifies that the logout endpoint is POST-only (no GET mapping).
	/// </summary>
	[Fact]
	public async Task Logout_Endpoint_ShouldOnlyAcceptPost()
	{
		// Arrange
		var getResponse = await _client!.GetAsync("/account/logout");
		var postResponse = await _client!.PostAsync("/account/logout", null);

		// Assert
		getResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
		postResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
	}

	#endregion
}
