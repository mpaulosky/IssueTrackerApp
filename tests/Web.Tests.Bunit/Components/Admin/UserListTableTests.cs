// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserListTableTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Domain.Features.Admin.Models;

using Web.Components.Admin.Users;

namespace Web.Tests.Bunit.Components.Admin;

/// <summary>
///   bUnit tests for the UserListTable component.
/// </summary>
public class UserListTableTests : BunitTestBase
{
	private static AdminUserSummary CreateAdminUser(
		string userId = "user-1",
		string name = "Test User",
		string email = "test@example.com",
		IReadOnlyList<string>? roles = null,
		bool isBlocked = false) =>
		new()
		{
			UserId = userId,
			Name = name,
			Email = email,
			Roles = roles ?? ["User"],
			IsBlocked = isBlocked,
			LastLogin = DateTimeOffset.UtcNow.AddDays(-1)
		};

	#region Empty State Tests

	[Fact]
	public void UserListTable_WithNullUsers_ShowsEmptyState()
	{
		// Arrange & Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, (IReadOnlyList<AdminUserSummary>)null!));

		// Assert
		cut.Markup.Should().Contain("No users found.", "null Users should show empty state");
	}

	[Fact]
	public void UserListTable_WithEmptyUsers_ShowsEmptyState()
	{
		// Arrange & Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, Array.Empty<AdminUserSummary>()));

		// Assert
		cut.Markup.Should().Contain("No users found.", "empty Users list should show empty state");
	}

	#endregion

	#region User Row Rendering Tests

	[Fact]
	public void UserListTable_WithUsers_RendersUserName()
	{
		// Arrange
		var users = new[] { CreateAdminUser(name: "Alice Smith") };

		// Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users));

		// Assert
		cut.Markup.Should().Contain("Alice Smith", "user name should be rendered in table");
	}

	[Fact]
	public void UserListTable_WithUsers_RendersUserEmail()
	{
		// Arrange
		var users = new[] { CreateAdminUser(email: "alice@example.com") };

		// Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users));

		// Assert
		cut.Markup.Should().Contain("alice@example.com", "user email should be rendered in table");
	}

	[Fact]
	public void UserListTable_WithMultipleUsers_RendersAllRows()
	{
		// Arrange
		var users = new[]
		{
			CreateAdminUser(userId: "u1", name: "Alice"),
			CreateAdminUser(userId: "u2", name: "Bob"),
			CreateAdminUser(userId: "u3", name: "Charlie")
		};

		// Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users));

		// Assert
		var rows = cut.FindAll("tbody tr");
		rows.Should().HaveCount(3, "three users should produce three rows");
	}

	[Fact]
	public void UserListTable_WithUserRoles_RendersRoleNames()
	{
		// Arrange
		var users = new[] { CreateAdminUser(roles: ["Admin", "User"]) };

		// Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users));

		// Assert
		cut.Markup.Should().Contain("Admin");
		cut.Markup.Should().Contain("User");
	}

	[Fact]
	public void UserListTable_WithNoRoles_RendersNoRolesText()
	{
		// Arrange
		var users = new[] { CreateAdminUser(roles: []) };

		// Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users));

		// Assert
		cut.Markup.Should().Contain("No roles", "user with no roles should show 'No roles'");
	}

	[Fact]
	public void UserListTable_WithBlockedUser_RendersBlockedBadge()
	{
		// Arrange
		var users = new[] { CreateAdminUser(isBlocked: true) };

		// Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users));

		// Assert
		cut.Markup.Should().Contain("Blocked", "blocked user should display 'Blocked' badge");
	}

	#endregion

	#region Edit Roles Callback Tests

	[Fact]
	public async Task UserListTable_EditRolesButton_InvokesOnEditRolesCallback()
	{
		// Arrange
		AdminUserSummary? capturedUser = null;
		var user = CreateAdminUser(userId: "user-edit-test", name: "Edit Target");
		var users = new[] { user };

		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users)
			.Add(p => p.OnEditRoles, EventCallback.Factory.Create<AdminUserSummary>(
				this, u => capturedUser = u)));

		// Act
		var editButton = cut.Find("button:first-child");
		await cut.InvokeAsync(() => editButton.Click());

		// Assert
		capturedUser.Should().NotBeNull("OnEditRoles should be invoked when Edit Roles is clicked");
		capturedUser!.UserId.Should().Be("user-edit-test");
	}

	[Fact]
	public async Task UserListTable_AuditLogButton_InvokesOnViewAuditLogCallback()
	{
		// Arrange
		string? capturedUserId = null;
		var user = CreateAdminUser(userId: "audit-user-id");
		var users = new[] { user };

		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users)
			.Add(p => p.OnViewAuditLog, EventCallback.Factory.Create<string>(
				this, id => capturedUserId = id)));

		// Act — the Audit Log button is the second button in the actions cell
		var actionButtons = cut.FindAll("td button");
		await cut.InvokeAsync(() => actionButtons[1].Click());

		// Assert
		capturedUserId.Should().Be("audit-user-id", "OnViewAuditLog should receive the correct userId");
	}

	#endregion

	#region Action Button Render Tests

	[Fact]
	public void UserListTable_WithUsers_RendersEditRolesButton()
	{
		// Arrange
		var users = new[] { CreateAdminUser() };

		// Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users));

		// Assert
		cut.Markup.Should().Contain("Edit Roles", "Edit Roles button should be present");
	}

	[Fact]
	public void UserListTable_WithUsers_RendersAuditLogButton()
	{
		// Arrange
		var users = new[] { CreateAdminUser() };

		// Act
		var cut = Render<UserListTable>(parameters => parameters
			.Add(p => p.Users, users));

		// Assert
		cut.Markup.Should().Contain("Audit Log", "Audit Log button should be present");
	}

	#endregion
}
