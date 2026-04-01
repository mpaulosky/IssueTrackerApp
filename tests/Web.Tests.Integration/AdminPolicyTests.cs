// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AdminPolicyTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests verifying that AdminPolicy is enforced on all /admin routes.
///   Covers Issue #143: Admin policy enforcement and /admin route protection.
/// </summary>
/// <remarks>
///   Authentication is provided by <see cref="TestAuthHandler"/>:
///   <list type="bullet">
///     <item><description>Anonymous: <c>X-Test-Anonymous: true</c> → unauthenticated → 401</description></item>
///     <item><description>User role: <c>X-Test-Role: User</c> → authenticated, no Admin role → 403</description></item>
///     <item><description>Admin role: <c>X-Test-Role: Admin</c> → authenticated Admin → 200</description></item>
///   </list>
/// </remarks>
[Collection("Integration")]
public sealed class AdminPolicyTests : IntegrationTestBase
{
	public AdminPolicyTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	// ── /admin (dashboard index) ─────────────────────────────────────────

	#region GET /admin - Admin Dashboard

	[Fact]
	public async Task GetAdminDashboard_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/admin");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetAdminDashboard_AuthenticatedWithoutAdminRole_ReturnsForbidden()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("User");

		// Act
		var response = await client.GetAsync("/admin");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task GetAdminDashboard_AuthenticatedWithAdminRole_ReturnsOk()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.GetAsync("/admin");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	#endregion

	// ── /admin/users ──────────────────────────────────────────────────────

	#region GET /admin/users - User Management

	[Fact]
	public async Task GetAdminUsersPage_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/admin/users");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetAdminUsersPage_AuthenticatedWithoutAdminRole_ReturnsForbidden()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("User");

		// Act
		var response = await client.GetAsync("/admin/users");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task GetAdminUsersPage_AuthenticatedWithAdminRole_ReturnsOk()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.GetAsync("/admin/users");

		// Assert
		// 200 is returned regardless of whether UserManagementService
		// successfully loads Auth0 users; component-level errors are
		// rendered inside the page rather than changing the HTTP status code.
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	#endregion

	// ── /admin/categories ────────────────────────────────────────────────

	#region GET /admin/categories - Category Management

	[Fact]
	public async Task GetAdminCategoriesPage_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/admin/categories");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetAdminCategoriesPage_AuthenticatedWithoutAdminRole_ReturnsForbidden()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("User");

		// Act
		var response = await client.GetAsync("/admin/categories");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task GetAdminCategoriesPage_AuthenticatedWithAdminRole_ReturnsOk()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.GetAsync("/admin/categories");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	#endregion

	// ── /admin/statuses ───────────────────────────────────────────────────

	#region GET /admin/statuses - Status Management

	[Fact]
	public async Task GetAdminStatusesPage_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/admin/statuses");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetAdminStatusesPage_AuthenticatedWithoutAdminRole_ReturnsForbidden()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("User");

		// Act
		var response = await client.GetAsync("/admin/statuses");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task GetAdminStatusesPage_AuthenticatedWithAdminRole_ReturnsOk()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.GetAsync("/admin/statuses");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	#endregion

	// ── /admin/analytics ─────────────────────────────────────────────────

	#region GET /admin/analytics - Analytics Dashboard

	[Fact]
	public async Task GetAdminAnalyticsPage_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/admin/analytics");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetAdminAnalyticsPage_AuthenticatedWithoutAdminRole_ReturnsForbidden()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("User");

		// Act
		var response = await client.GetAsync("/admin/analytics");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task GetAdminAnalyticsPage_AuthenticatedWithAdminRole_ReturnsOk()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.GetAsync("/admin/analytics");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	#endregion

	// ── AdminPolicy on mutating API endpoints ────────────────────────────

	#region POST /api/categories - AdminPolicy enforcement

	[Fact]
	public async Task CreateCategoryViaApi_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange – anonymous caller cannot invoke admin-only API operations
		using var client = CreateAnonymousClient();
		var body = new { CategoryName = "TestCat", CategoryDescription = "desc" };

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", body);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task CreateCategoryViaApi_AuthenticatedWithoutAdminRole_ReturnsForbidden()
	{
		// Arrange – authenticated user without Admin role must be rejected
		using var client = CreateAuthenticatedClient("User");
		var body = new { CategoryName = "TestCat", CategoryDescription = "desc" };

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", body);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task CreateCategoryViaApi_AuthenticatedWithAdminRole_Succeeds()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");
		var body = new { CategoryName = "AdminCreated", CategoryDescription = "Created by admin" };

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", body);

		// Assert – admin user receives 201 Created (or 409 if already exists, but not 4xx auth errors)
		response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
	}

	#endregion

	#region PUT /api/categories/{id} - AdminPolicy enforcement

	[Fact]
	public async Task UpdateCategoryViaApi_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var categories = await SeedCategoriesAsync();
		var target = categories.First();
		using var client = CreateAnonymousClient();
		var body = new { CategoryName = "Updated", CategoryDescription = "updated desc" };

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{target.Id}", body);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task UpdateCategoryViaApi_AuthenticatedWithoutAdminRole_ReturnsForbidden()
	{
		// Arrange
		var categories = await SeedCategoriesAsync();
		var target = categories.First();
		using var client = CreateAuthenticatedClient("User");
		var body = new { CategoryName = "Updated", CategoryDescription = "updated desc" };

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{target.Id}", body);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	#endregion

	#region DELETE /api/categories/{id} - AdminPolicy enforcement

	[Fact]
	public async Task ArchiveCategoryViaApi_Unauthenticated_ReturnsUnauthorized()
	{
		// Arrange
		var categories = await SeedCategoriesAsync();
		var target = categories.First();
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.DeleteAsync($"/api/categories/{target.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ArchiveCategoryViaApi_AuthenticatedWithoutAdminRole_ReturnsForbidden()
	{
		// Arrange
		var categories = await SeedCategoriesAsync();
		var target = categories.First();
		using var client = CreateAuthenticatedClient("User");

		// Act
		var response = await client.DeleteAsync($"/api/categories/{target.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	#endregion
}
