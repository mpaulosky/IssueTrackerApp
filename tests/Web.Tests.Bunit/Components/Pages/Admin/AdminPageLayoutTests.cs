// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AdminPageLayoutTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using System.Reflection;

using Microsoft.AspNetCore.Components;

using Web.Components.Pages.Admin;

namespace Web.Tests.Bunit.Components.Pages.Admin;

/// <summary>
///   Regression tests for AdminPageLayout component.
///   Guards against incorrect use as @layout directive (issue #97).
/// </summary>
public class AdminPageLayoutTests : BunitTestBase
{
	[Fact]
	public void AdminPageLayout_RendersChildContent_WhenProvided()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		const string testContent = "Hello from child content";

		// Act
		var cut = Render<AdminPageLayout>(parameters => parameters
			.AddChildContent($"<p id=\"test\">{testContent}</p>")
		);

		// Assert
		cut.Markup.Should().Contain(testContent,
			"child content should be rendered in the component");
		var testElement = cut.Find("#test");
		testElement.Should().NotBeNull();
		testElement.TextContent.Should().Be(testContent);
	}

	[Fact]
	public void AdminPageLayout_RendersTitle_WhenTitleProvided()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		const string testTitle = "Test Page Title";

		// Act
		var cut = Render<AdminPageLayout>(parameters => parameters
			.Add(p => p.Title, testTitle)
		);

		// Assert
		var h1 = cut.Find("h1");
		h1.Should().NotBeNull();
		h1.TextContent.Should().Contain(testTitle,
			"title should be rendered in h1 element");
	}

	[Fact]
	public void AdminPageLayout_RendersDescription_WhenDescriptionProvided()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		const string testTitle = "Test Page";
		const string testDescription = "This is a test description";

		// Act
		var cut = Render<AdminPageLayout>(parameters => parameters
			.Add(p => p.Title, testTitle)
			.Add(p => p.Description, testDescription)
		);

		// Assert
		cut.Markup.Should().Contain(testDescription,
			"description should be rendered when provided");
	}

	[Fact]
	public void AdminPageLayout_DoesNotRenderTitleSection_WhenTitleIsNull()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);

		// Act
		var cut = Render<AdminPageLayout>(parameters => parameters
			.AddChildContent("<p>Content without title</p>")
		);

		// Assert
		var h1Elements = cut.FindAll("h1");
		h1Elements.Should().BeEmpty(
			"title section should not be rendered when Title is null");
	}

	[Fact]
	public void AdminPageLayout_DoesNotInheritLayoutComponentBase()
	{
		// Arrange & Act
		var adminPageLayoutType = typeof(AdminPageLayout);
		var isLayoutComponent = adminPageLayoutType.IsAssignableTo(typeof(LayoutComponentBase));

		// Assert
		isLayoutComponent.Should().BeFalse(
			"AdminPageLayout must NOT inherit from LayoutComponentBase to prevent @layout directive misuse (issue #97)");
	}

	[Fact]
	public void AdminPageLayout_HasChildContentParameter_NoBodyParameter()
	{
		// Arrange
		var adminPageLayoutType = typeof(AdminPageLayout);

		// Act
		var childContentProperty = adminPageLayoutType.GetProperty("ChildContent");
		var bodyProperty = adminPageLayoutType.GetProperty("Body");

		// Assert
		childContentProperty.Should().NotBeNull(
			"AdminPageLayout must have a ChildContent parameter");
		childContentProperty!.PropertyType.Should().Be(typeof(RenderFragment),
			"ChildContent should be of type RenderFragment");

		bodyProperty.Should().BeNull(
			"AdminPageLayout must NOT have a Body parameter (that's for LayoutComponentBase)");
	}

	[Fact]
	public void AdminPageLayout_RendersAdminPortalNavigation()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);

		// Act
		var cut = Render<AdminPageLayout>();

		// Assert
		cut.Markup.Should().Contain("Admin Portal",
			"admin navigation bar should display 'Admin Portal' branding");
		cut.Markup.Should().Contain("href=\"/admin\"",
			"admin navigation should link to /admin");
	}

	[Fact]
	public void AdminPageLayout_RendersBackToAppLink()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);

		// Act
		var cut = Render<AdminPageLayout>();

		// Assert
		cut.Markup.Should().Contain("Back to App",
			"admin layout should provide 'Back to App' link");
		cut.Markup.Should().Contain("href=\"/\"",
			"'Back to App' should link to home page");
	}

	[Fact]
	public void AdminPageLayout_RendersNavigationLinks()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);

		// Act
		var cut = Render<AdminPageLayout>();

		// Assert
		cut.Markup.Should().Contain("Dashboard", "nav should contain Dashboard link");
		cut.Markup.Should().Contain("Analytics", "nav should contain Analytics link");
		cut.Markup.Should().Contain("Categories", "nav should contain Categories link");
		cut.Markup.Should().Contain("Statuses", "nav should contain Statuses link");
	}

	[Fact]
	public void AdminPageLayout_WrapsContentInMainElement()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		const string testContent = "Test content in main";

		// Act
		var cut = Render<AdminPageLayout>(parameters => parameters
			.AddChildContent($"<p>{testContent}</p>")
		);

		// Assert
		var mainElement = cut.Find("main");
		mainElement.Should().NotBeNull();
		mainElement.TextContent.Should().Contain(testContent,
			"child content should be rendered inside <main> element");
	}

	[Fact]
	public void AdminPageLayout_HasParameterAttributes()
	{
		// Arrange
		var adminPageLayoutType = typeof(AdminPageLayout);

		// Act
		var titleProperty = adminPageLayoutType.GetProperty("Title");
		var descriptionProperty = adminPageLayoutType.GetProperty("Description");
		var childContentProperty = adminPageLayoutType.GetProperty("ChildContent");

		// Assert — all properties should have [Parameter] attribute
		titleProperty.Should().NotBeNull();
		titleProperty!.GetCustomAttribute<ParameterAttribute>().Should().NotBeNull(
			"Title property should have [Parameter] attribute");

		descriptionProperty.Should().NotBeNull();
		descriptionProperty!.GetCustomAttribute<ParameterAttribute>().Should().NotBeNull(
			"Description property should have [Parameter] attribute");

		childContentProperty.Should().NotBeNull();
		childContentProperty!.GetCustomAttribute<ParameterAttribute>().Should().NotBeNull(
			"ChildContent property should have [Parameter] attribute");
	}

	[Fact]
	public void AdminPageLayout_TitleAndDescriptionAreNullable()
	{
		// Arrange
		var adminPageLayoutType = typeof(AdminPageLayout);

		// Act
		var titleProperty = adminPageLayoutType.GetProperty("Title");
		var descriptionProperty = adminPageLayoutType.GetProperty("Description");

		// Assert
		titleProperty.Should().NotBeNull();
		var titleNullabilityContext = new NullabilityInfoContext();
		var titleNullability = titleNullabilityContext.Create(titleProperty!);
		titleNullability.WriteState.Should().Be(NullabilityState.Nullable,
			"Title property should be nullable");

		descriptionProperty.Should().NotBeNull();
		var descriptionNullability = titleNullabilityContext.Create(descriptionProperty!);
		descriptionNullability.WriteState.Should().Be(NullabilityState.Nullable,
			"Description property should be nullable");
	}

	[Fact]
	public void AdminPageLayout_RendersWithBothTitleAndChildContent()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		const string testTitle = "Admin Page";
		const string testContent = "Page content here";

		// Act
		var cut = Render<AdminPageLayout>(parameters => parameters
			.Add(p => p.Title, testTitle)
			.AddChildContent($"<div id=\"content\">{testContent}</div>")
		);

		// Assert
		cut.Markup.Should().Contain(testTitle, "title should be rendered");
		cut.Markup.Should().Contain(testContent, "child content should be rendered");

		var h1 = cut.Find("h1");
		h1.TextContent.Should().Contain(testTitle);

		var contentDiv = cut.Find("#content");
		contentDiv.TextContent.Should().Be(testContent);
	}

	[Fact]
	public void AdminPageLayout_DescriptionNotRendered_WhenTitleIsNullButDescriptionProvided()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		const string testDescription = "Orphaned description";

		// Act
		var cut = Render<AdminPageLayout>(parameters => parameters
			.Add(p => p.Description, testDescription)
		);

		// Assert
		cut.Markup.Should().NotContain(testDescription,
			"description should not render when Title is null (entire title section is skipped)");
	}
}
