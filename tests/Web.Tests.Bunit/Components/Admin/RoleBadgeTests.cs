// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoleBadgeTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Web.Components.Admin.Users;

namespace Web.Tests.Bunit.Components.Admin;

/// <summary>
///   bUnit tests for the RoleBadge component.
/// </summary>
public class RoleBadgeTests : BunitTestBase
{
	#region Renders Nothing Tests

	[Fact]
	public void RoleBadge_WithNullRoleName_RendersNothing()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, null));

		// Assert
		cut.Markup.Trim().Should().BeEmpty("null RoleName should render nothing");
	}

	[Fact]
	public void RoleBadge_WithEmptyRoleName_RendersNothing()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, string.Empty));

		// Assert
		cut.Markup.Trim().Should().BeEmpty("empty RoleName should render nothing");
	}

	[Fact]
	public void RoleBadge_WithWhitespaceRoleName_RendersNothing()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "   "));

		// Assert
		cut.Markup.Trim().Should().BeEmpty("whitespace-only RoleName should render nothing");
	}

	#endregion

	#region CSS Color Class Tests

	[Fact]
	public void RoleBadge_AdminRole_RendersRedColorClasses()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "Admin"));

		// Assert
		var span = cut.Find("span");
		span.ClassList.Should().Contain("bg-red-100", "Admin role should have red background");
		span.ClassList.Should().Contain("text-red-800", "Admin role should have red text");
	}

	[Fact]
	public void RoleBadge_ModeratorRole_RendersOrangeColorClasses()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "Moderator"));

		// Assert
		var span = cut.Find("span");
		span.ClassList.Should().Contain("bg-orange-100", "Moderator role should have orange background");
		span.ClassList.Should().Contain("text-orange-800", "Moderator role should have orange text");
	}

	[Fact]
	public void RoleBadge_UserRole_RendersBlueColorClasses()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "User"));

		// Assert
		var span = cut.Find("span");
		span.ClassList.Should().Contain("bg-blue-100", "User role should have blue background");
		span.ClassList.Should().Contain("text-blue-800", "User role should have blue text");
	}

	[Fact]
	public void RoleBadge_UnknownRole_RendersGreyColorClasses()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "CustomRole"));

		// Assert
		var span = cut.Find("span");
		span.ClassList.Should().Contain("bg-gray-100", "Unknown role should have grey background");
		span.ClassList.Should().Contain("text-gray-800", "Unknown role should have grey text");
	}

	[Fact]
	public void RoleBadge_AdminRole_CaseInsensitive_RendersRedColorClasses()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "admin"));

		// Assert
		var span = cut.Find("span");
		span.ClassList.Should().Contain("bg-red-100", "Role lookup should be case-insensitive");
	}

	#endregion

	#region Render Content Tests

	[Fact]
	public void RoleBadge_WithRoleName_RendersRoleNameText()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "Admin"));

		// Assert
		var span = cut.Find("span");
		span.TextContent.Should().Be("Admin");
	}

	[Fact]
	public void RoleBadge_WithRoleName_HasBadgeBaseClass()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "User"));

		// Assert
		var span = cut.Find("span");
		span.ClassList.Should().Contain("badge");
	}

	[Fact]
	public void RoleBadge_WithRoleName_HasAriaLabel()
	{
		// Arrange & Act
		var cut = Render<RoleBadge>(parameters => parameters
			.Add(p => p.RoleName, "Admin"));

		// Assert
		var span = cut.Find("span");
		span.GetAttribute("aria-label").Should().Be("Role: Admin");
	}

	#endregion
}
