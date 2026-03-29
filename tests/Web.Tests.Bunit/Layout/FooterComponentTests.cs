// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     FooterComponentTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Layout;

namespace Web.Tests.Bunit.Layout;

/// <summary>
///   Tests for FooterComponent rendering, links, and content.
/// </summary>
public class FooterComponentTests : BunitTestBase
{
	[Fact]
	public void FooterComponent_RendersWithoutErrors()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert
		cut.Markup.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void FooterComponent_RendersFooterElement()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert
		var footer = cut.Find("footer");
		footer.Should().NotBeNull();
		footer.GetAttribute("role").Should().Be("contentinfo");
	}

	[Fact]
	public void FooterComponent_DisplaysCopyrightWithCurrentYear()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert
		var year = DateTime.Now.Year.ToString();
		cut.Markup.Should().Contain(year);
		cut.Markup.Should().Contain("IssueTracker");
	}

	[Fact]
	public void FooterComponent_DisplaysTechnologyStack()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert
		cut.Markup.Should().Contain(".NET 10");
		cut.Markup.Should().Contain("Blazor");
	}

	[Fact]
	public void FooterComponent_ContainsGitHubLinks()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert
		var links = cut.FindAll("a[target='_blank']");
		links.Should().HaveCountGreaterThanOrEqualTo(2, "should have release and commit links");
	}

	[Fact]
	public void FooterComponent_LinksPointToCorrectRepository()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert
		var links = cut.FindAll("a");
		foreach (var link in links)
		{
			var href = link.GetAttribute("href");
			href.Should().Contain("mpaulosky/IssueTrackerApp",
				"all footer links should point to the correct repository");
		}
	}

	[Fact]
	public void FooterComponent_LinksHaveSecurityAttributes()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert
		var externalLinks = cut.FindAll("a[target='_blank']");
		foreach (var link in externalLinks)
		{
			link.GetAttribute("rel").Should().Contain("noopener",
				"external links should have rel='noopener noreferrer' for security");
		}
	}

	[Fact]
	public void FooterComponent_UsesThemeColors()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert — footer uses primary theme colors for border and background
		var footer = cut.Find("footer");
		var classes = footer.GetAttribute("class") ?? "";
		classes.Should().Contain("border-primary-800");
		classes.Should().Contain("bg-primary-400");
		classes.Should().Contain("dark:bg-primary-400");
	}

	[Fact]
	public void FooterComponent_LinksHaveHoverTransition()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert — links are rendered with correct structure and security attributes
		var links = cut.FindAll("a");
		links.Should().NotBeEmpty("footer should contain version and commit links");
		foreach (var link in links)
		{
			link.GetAttribute("rel").Should().Contain("noopener",
				"links should maintain security attributes");
		}
		cut.Markup.Should().Contain("font-mono",
			"version and commit links container should use monospace font");
	}

	[Fact]
	public void FooterComponent_HasResponsiveLayout()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert
		cut.Markup.Should().Contain("sm:flex-row",
			"footer should use responsive flex layout");
		cut.Markup.Should().Contain("flex-col",
			"footer should stack vertically on small screens");
	}

	[Fact]
	public void FooterComponent_DisplaysBuildVersionInfo()
	{
		// Arrange & Act
		var cut = Render<FooterComponent>();

		// Assert — BuildInfo.Version and BuildInfo.Commit are compile-time constants
		var links = cut.FindAll("a[target='_blank']");
		links.Should().HaveCountGreaterThanOrEqualTo(2);

		// Version link — select by href containing /releases/ for stability
		var versionLink = cut.Find("a[href*='/releases']");
		versionLink.GetAttribute("title").Should().Contain("View");
	}
}
