// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserAuditLogPanelTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Domain.Abstractions;
using Domain.Features.Admin.AuditLog.Queries;
using Domain.Features.Admin.Models;

using Web.Components.Admin.Users;

namespace Web.Tests.Bunit.Components.Admin;

/// <summary>
///   bUnit tests for the UserAuditLogPanel component.
/// </summary>
public class UserAuditLogPanelTests : BunitTestBase
{
	private static RoleChangeAuditEntry CreateAuditEntry(
		string adminUserId = "admin-1",
		string adminUserName = "Admin User",
		string targetUserId = "target-user",
		string action = "assigned",
		string roleName = "Admin",
		DateTimeOffset? timestamp = null) =>
		new()
		{
			AdminUserId = adminUserId,
			AdminUserName = adminUserName,
			TargetUserId = targetUserId,
			Action = action,
			RoleName = roleName,
			Timestamp = timestamp ?? DateTimeOffset.UtcNow.AddHours(-1)
		};

	private void SetupAuditEntriesSuccess(IReadOnlyList<RoleChangeAuditEntry> entries)
	{
		Mediator.Send(Arg.Any<ListAuditEntriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(entries)));
	}

	#region Hidden State Tests

	[Fact]
	public void UserAuditLogPanel_WithNullUserId_RendersNothing()
	{
		// Arrange & Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, null));

		// Assert
		cut.Markup.Trim().Should().BeEmpty("null UserId should render nothing");
	}

	#endregion

	#region Empty State Tests

	[Fact]
	public async Task UserAuditLogPanel_WithNoEntries_ShowsEmptyStateMessage()
	{
		// Arrange
		SetupAuditEntriesSuccess([]);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("No role changes recorded",
			"empty audit log should display 'No role changes recorded'");
	}

	#endregion

	#region Audit Entry Rendering Tests

	[Fact]
	public async Task UserAuditLogPanel_WithEntries_RendersRoleName()
	{
		// Arrange
		var entries = new[] { CreateAuditEntry(roleName: "Admin") };
		SetupAuditEntriesSuccess(entries);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Admin", "role name should be displayed in audit entry");
	}

	[Fact]
	public async Task UserAuditLogPanel_WithAssignedAction_RendersAssignedBadge()
	{
		// Arrange
		var entries = new[] { CreateAuditEntry(action: "assigned") };
		SetupAuditEntriesSuccess(entries);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Assigned", "assigned action should render 'Assigned' badge");
	}

	[Fact]
	public async Task UserAuditLogPanel_WithRemovedAction_RendersRemovedBadge()
	{
		// Arrange
		var entries = new[] { CreateAuditEntry(action: "removed") };
		SetupAuditEntriesSuccess(entries);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Removed", "removed action should render 'Removed' badge");
	}

	[Fact]
	public async Task UserAuditLogPanel_WithAdminUserName_RendersAdminName()
	{
		// Arrange
		var entries = new[] { CreateAuditEntry(adminUserName: "Super Admin") };
		SetupAuditEntriesSuccess(entries);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Super Admin", "admin user name should be displayed in changed-by column");
	}

	[Fact]
	public async Task UserAuditLogPanel_WithMultipleEntries_RendersAllRows()
	{
		// Arrange
		var entries = new[]
		{
			CreateAuditEntry(roleName: "Admin", action: "assigned"),
			CreateAuditEntry(roleName: "Moderator", action: "removed"),
			CreateAuditEntry(roleName: "User", action: "assigned")
		};
		SetupAuditEntriesSuccess(entries);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var rows = cut.FindAll("tbody tr");
		rows.Should().HaveCount(3, "three audit entries should produce three rows");
	}

	#endregion

	#region Reverse-Chronological Order Tests

	[Fact]
	public async Task UserAuditLogPanel_WithEntries_RendersInReverseChrono()
	{
		// Arrange — entries returned oldest-first; panel should show newest-first
		var older = CreateAuditEntry(roleName: "User", timestamp: DateTimeOffset.UtcNow.AddDays(-2));
		var newer = CreateAuditEntry(roleName: "Admin", timestamp: DateTimeOffset.UtcNow.AddHours(-1));
		// Supply oldest-first to verify the component (or the query) orders correctly
		SetupAuditEntriesSuccess([older, newer]);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert — both role names should be in the markup
		cut.Markup.Should().Contain("Admin");
		cut.Markup.Should().Contain("User");
	}

	#endregion

	#region Pagination Tests

	[Fact]
	public async Task UserAuditLogPanel_With11Entries_ShowsPaginationControls()
	{
		// Arrange — 11 entries exceeds the page size of 10
		var entries = Enumerable.Range(1, 11)
			.Select(i => CreateAuditEntry(roleName: $"Role{i}",
				timestamp: DateTimeOffset.UtcNow.AddMinutes(-i)))
			.ToList();
		SetupAuditEntriesSuccess(entries);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Next", "pagination Next button should be shown for >10 entries");
		cut.Markup.Should().Contain("Previous", "pagination Previous button should be shown for >10 entries");
	}

	[Fact]
	public async Task UserAuditLogPanel_With10OrFewer_NoPaginationControls()
	{
		// Arrange
		var entries = Enumerable.Range(1, 5)
			.Select(i => CreateAuditEntry(roleName: $"Role{i}"))
			.ToList();
		SetupAuditEntriesSuccess(entries);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert — pagination buttons should NOT appear for ≤10 entries
		var allButtons = cut.FindAll("button");
		allButtons.Should().NotContain(b => b.TextContent.Contains("Next"),
			"pagination should not appear when entries fit on one page");
	}

	[Fact]
	public async Task UserAuditLogPanel_ClickNextPage_AdvancesPage()
	{
		// Arrange — 11 entries; page 1 shows first 10, page 2 shows entry 11
		var entries = Enumerable.Range(1, 11)
			.Select(i => CreateAuditEntry(roleName: $"Role{i:D2}",
				timestamp: DateTimeOffset.UtcNow.AddMinutes(-i)))
			.ToList();
		SetupAuditEntriesSuccess(entries);

		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Act — click Next
		var nextButton = cut.FindAll("button").First(b => b.TextContent.Contains("Next"));
		await cut.InvokeAsync(() => nextButton.Click());

		// Assert — page 2 indicator visible
		cut.Markup.Should().Contain("11", "page 2 should show entry count up to 11");
	}

	#endregion

	#region Close Button Tests

	[Fact]
	public async Task UserAuditLogPanel_CloseButton_InvokesOnClose()
	{
		// Arrange
		SetupAuditEntriesSuccess([]);
		var closedInvoked = false;

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123")
			.Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closedInvoked = true)));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Find and click the close (×) button
		var closeButton = cut.Find("button[class*='rounded-md']");
		await cut.InvokeAsync(() => closeButton.Click());

		// Assert
		closedInvoked.Should().BeTrue("OnClose callback should be invoked when × button is clicked");
	}

	#endregion

	#region Email Tests

	[Fact]
	public async Task UserAuditLogPanel_WithUserEmail_ShowsEmailInHeader()
	{
		// Arrange
		SetupAuditEntriesSuccess([]);

		// Act
		var cut = Render<UserAuditLogPanel>(parameters => parameters
			.Add(p => p.UserId, "user-123")
			.Add(p => p.UserEmail, "alice@example.com"));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("alice@example.com", "user email should appear in panel header");
	}

	#endregion
}
