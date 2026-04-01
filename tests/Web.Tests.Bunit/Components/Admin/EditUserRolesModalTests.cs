// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     EditUserRolesModalTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Domain.Abstractions;
using Domain.Features.Admin.Models;
using Domain.Features.Admin.Users.Commands;
using Domain.Features.Admin.Users.Queries;

using MediatR;

using Web.Components.Admin.Users;

namespace Web.Tests.Bunit.Components.Admin;

/// <summary>
///   bUnit tests for the EditUserRolesModal component.
/// </summary>
public class EditUserRolesModalTests : BunitTestBase
{
	private static AdminUserSummary CreateAdminUser(
		string userId = "target-user-id",
		string name = "Target User",
		string email = "target@example.com",
		IReadOnlyList<string>? roles = null) =>
		new()
		{
			UserId = userId,
			Name = name,
			Email = email,
			Roles = roles ?? ["User"],
			LastLogin = DateTimeOffset.UtcNow.AddDays(-1)
		};

	private static IReadOnlyList<RoleAssignment> CreateAvailableRoles() =>
	[
		new RoleAssignment { RoleId = "r1", RoleName = "Admin", Description = "Administrator" },
		new RoleAssignment { RoleId = "r2", RoleName = "Moderator", Description = "Content moderator" },
		new RoleAssignment { RoleId = "r3", RoleName = "User", Description = "Standard user" }
	];

	private void SetupListRolesQuery(IReadOnlyList<RoleAssignment>? roles = null)
	{
		Mediator.Send(Arg.Any<ListRolesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(roles ?? CreateAvailableRoles())));
	}

	private void SetupAssignRoleSuccess()
	{
		Mediator.Send(Arg.Any<AssignRoleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(true)));
	}

	private void SetupRemoveRoleSuccess()
	{
		Mediator.Send(Arg.Any<RemoveRoleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(true)));
	}

	#region Hidden State Tests

	[Fact]
	public void EditUserRolesModal_WithNullUser_RendersNothing()
	{
		// Arrange & Act
		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, null));

		// Assert
		cut.Markup.Trim().Should().BeEmpty("null User should render nothing");
	}

	#endregion

	#region Role Checkbox Rendering Tests

	[Fact]
	public async Task EditUserRolesModal_WithUser_RendersAvailableRoleCheckboxes()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		var user = CreateAdminUser(roles: ["User"]);

		// Act
		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var checkboxes = cut.FindAll("input[type='checkbox']");
		checkboxes.Should().HaveCount(3, "three available roles should render three checkboxes");
	}

	[Fact]
	public async Task EditUserRolesModal_WithUser_CurrentRoleCheckboxIsChecked()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		var user = CreateAdminUser(roles: ["User"]);

		// Act
		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("User", "current user role should appear in the modal");
	}

	[Fact]
	public async Task EditUserRolesModal_WithUser_ShowsRoleLabels()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		var user = CreateAdminUser();

		// Act
		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Admin");
		cut.Markup.Should().Contain("Moderator");
		cut.Markup.Should().Contain("User");
	}

	#endregion

	#region Save Button Disabled When No Changes Tests

	[Fact]
	public async Task EditUserRolesModal_WithNoChanges_SaveButtonIsDisabled()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		var user = CreateAdminUser(roles: ["User"]);

		// Act
		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var saveButton = cut.FindAll("button")
			.FirstOrDefault(b => b.TextContent.Contains("Save Changes"));
		saveButton.Should().NotBeNull("Save Changes button should be present");
		saveButton!.HasAttribute("disabled").Should().BeTrue("Save button should be disabled when IsDirty=false");
	}

	#endregion

	#region Confirmation Step Tests

	[Fact]
	public async Task EditUserRolesModal_AfterRoleChange_SaveButtonIsEnabled()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		var user = CreateAdminUser(roles: ["User"]);

		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Act — uncheck User role to make IsDirty=true
		var checkboxes = cut.FindAll("input[type='checkbox']");
		var userCheckbox = checkboxes.FirstOrDefault(c =>
		{
			var label = c.ParentElement;
			return label?.TextContent.Contains("User") == true;
		});
		userCheckbox.Should().NotBeNull();
		await cut.InvokeAsync(() => userCheckbox!.Change(false));

		// Assert
		var saveButton = cut.FindAll("button")
			.FirstOrDefault(b => b.TextContent.Contains("Save Changes"));
		saveButton.Should().NotBeNull();
		saveButton!.HasAttribute("disabled").Should().BeFalse("Save button should be enabled when IsDirty=true");
	}

	[Fact]
	public async Task EditUserRolesModal_ClickSaveChanges_ShowsConfirmationStep()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		var user = CreateAdminUser(roles: ["User"]);

		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Make a change — uncheck User role
		var checkboxes = cut.FindAll("input[type='checkbox']");
		var userCheckbox = checkboxes.FirstOrDefault(c =>
			c.ParentElement?.TextContent.Contains("User") == true);
		await cut.InvokeAsync(() => userCheckbox!.Change(false));

		// Act — click Save Changes
		var saveButton = cut.FindAll("button")
			.First(b => b.TextContent.Contains("Save Changes"));
		await cut.InvokeAsync(() => saveButton.Click());

		// Assert — confirmation step should be visible
		cut.Markup.Should().Contain("following role changes", "confirmation step should appear after clicking Save Changes");
	}

	[Fact]
	public async Task EditUserRolesModal_ConfirmationStep_ShowsRolesToRemove()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		var user = CreateAdminUser(roles: ["User"]);

		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Uncheck User role
		var checkboxes = cut.FindAll("input[type='checkbox']");
		var userCheckbox = checkboxes.FirstOrDefault(c =>
			c.ParentElement?.TextContent.Contains("User") == true);
		await cut.InvokeAsync(() => userCheckbox!.Change(false));

		// Click Save Changes
		var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save Changes"));
		await cut.InvokeAsync(() => saveButton.Click());

		// Assert
		cut.Markup.Should().Contain("Remove", "confirmation should list roles to remove");
	}

	#endregion

	#region Command Dispatch Tests

	[Fact]
	public async Task EditUserRolesModal_ConfirmSave_DispatchesAssignRoleCommand()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		SetupAssignRoleSuccess();
		var user = CreateAdminUser(roles: []); // No current roles

		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Check Admin role
		var adminCheckbox = cut.FindAll("input[type='checkbox']")
			.FirstOrDefault(c => c.ParentElement?.TextContent.Contains("Admin") == true);
		await cut.InvokeAsync(() => adminCheckbox!.Change(true));

		// Navigate to confirmation
		var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save Changes"));
		await cut.InvokeAsync(() => saveButton.Click());

		// Act — Confirm
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Confirm"));
		await cut.InvokeAsync(() => confirmButton!.Click());
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await Mediator.Received(1).Send(
			Arg.Is<AssignRoleCommand>(cmd => cmd.RoleName == "Admin"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task EditUserRolesModal_ConfirmSave_DispatchesRemoveRoleCommand()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		SetupRemoveRoleSuccess();
		var user = CreateAdminUser(roles: ["User"]);

		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Uncheck User role
		var userCheckbox = cut.FindAll("input[type='checkbox']")
			.FirstOrDefault(c => c.ParentElement?.TextContent.Contains("User") == true);
		await cut.InvokeAsync(() => userCheckbox!.Change(false));

		// Navigate to confirmation
		var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save Changes"));
		await cut.InvokeAsync(() => saveButton.Click());

		// Act — Confirm
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Confirm"));
		await cut.InvokeAsync(() => confirmButton!.Click());
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await Mediator.Received(1).Send(
			Arg.Is<RemoveRoleCommand>(cmd => cmd.RoleName == "User"),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region Error Handling Tests

	[Fact]
	public async Task EditUserRolesModal_WhenAssignRoleFails_ShowsInlineError()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();

		Mediator.Send(Arg.Any<AssignRoleCommand>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<bool>("Auth0 rate limit exceeded")));

		var user = CreateAdminUser(roles: []);

		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Check Admin role
		var adminCheckbox = cut.FindAll("input[type='checkbox']")
			.FirstOrDefault(c => c.ParentElement?.TextContent.Contains("Admin") == true);
		await cut.InvokeAsync(() => adminCheckbox!.Change(true));

		// Navigate to confirmation and confirm
		var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save Changes"));
		await cut.InvokeAsync(() => saveButton.Click());
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Confirm"));
		await cut.InvokeAsync(() => confirmButton!.Click());
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Auth0 rate limit exceeded", "error message should be shown inline");
	}

	#endregion

	#region Cancel / Close Tests

	[Fact]
	public async Task EditUserRolesModal_CancelButton_InvokesOnCloseWhenClean()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		SetupListRolesQuery();
		var closedInvoked = false;
		var user = CreateAdminUser();

		var cut = Render<EditUserRolesModal>(parameters => parameters
			.Add(p => p.User, user)
			.Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closedInvoked = true)));

		await cut.InvokeAsync(() => Task.Delay(50));

		// Act — click Cancel (no changes made, so no JS confirm dialog)
		var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
		await cut.InvokeAsync(() => cancelButton!.Click());

		// Assert
		closedInvoked.Should().BeTrue("OnClose callback should be invoked when Cancel is clicked without changes");
	}

	#endregion
}
