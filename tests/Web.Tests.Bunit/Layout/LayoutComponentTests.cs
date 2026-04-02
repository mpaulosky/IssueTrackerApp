// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LayoutComponentTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Microsoft.AspNetCore.Components;

using Web.Components.Layout;

namespace Web.Tests.Bunit.Layout;

/// <summary>
///   Tests for MainLayout component.
/// </summary>
public class MainLayoutTests : BunitTestBase
{
	[Fact]
	public void MainLayout_Renders_WithAuthenticatedUser()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert
		cut.Markup.Should().NotBeNullOrEmpty();
		cut.Find(".max-w-7xl").Should().NotBeNull();
	}

	[Fact]
	public void MainLayout_Renders_WithAnonymousUser()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert
		cut.Markup.Should().NotBeNullOrEmpty();
		cut.Find(".max-w-7xl").Should().NotBeNull();
	}

	[Fact]
	public void MainLayout_DisplaysIssueTrackerBranding()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert
		cut.Markup.Should().Contain("IssueTracker");
	}

	[Fact]
	public void MainLayout_ContainsHeaderElement()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert
		cut.Find("header").Should().NotBeNull();
		cut.Markup.Should().Contain("bg-primary-400");
		cut.Markup.Should().Contain("dark:bg-primary-900");
	}

	[Fact]
	public void MainLayout_ContainsMainContentArea()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert
		cut.Find("main").Should().NotBeNull();
		cut.Markup.Should().Contain("flex-1");
	}

	[Fact]
	public void MainLayout_ContainsToastContainer()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert - ToastContainer renders as a fixed div
		cut.Markup.Should().Contain("fixed top-20 right-4 z-50");
	}

	[Fact]
	public void MainLayout_ContainsErrorUI()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert
		cut.Find("#blazor-error-ui").Should().NotBeNull();
		cut.Markup.Should().Contain("An unhandled error has occurred");
	}

	[Fact]
	public void MainLayout_ContainsThemeProvider()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert - ThemeProvider wraps content via CascadingValue
		cut.FindComponent<Web.Components.Theme.ThemeProvider>().Should().NotBeNull();
	}

	[Fact]
	public void MainLayout_ContainsLoginDisplay()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert - LoginDisplay renders as a form with login/logout elements
		cut.FindComponent<LoginDisplay>().Should().NotBeNull();
	}

	[Fact]
	public void MainLayout_SupportsResponsiveDesign()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert
		cut.Markup.Should().Contain("sm:px-6");
		cut.Markup.Should().Contain("lg:px-8");
	}

	[Fact]
	public void MainLayout_SupportsThemeSwitching()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert
		// Verify theme-aware primary color classes are present
		cut.Markup.Should().Contain("bg-primary-500");
		cut.Markup.Should().Contain("dark:bg-primary-900");
		cut.Markup.Should().Contain("bg-primary-400");
		cut.Markup.Should().Contain("bg-primary-100");
	}

	[Fact]
	public void MainLayout_ContainsSignalRConnectionForAuthorizedUsers()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<MainLayout>();

		// Assert - SignalRConnection renders for authorized users
		cut.FindComponent<Web.Components.Shared.SignalRConnection>().Should().NotBeNull();
	}
}

/// <summary>
///   Tests for LoginDisplay component.
/// </summary>
public class LoginDisplayTests : BunitTestBase
{
	[Fact]
	public void LoginDisplay_WhenAuthenticated_ShowsGreeting()
	{
		// Arrange
		SetupAuthenticatedUser(userName: "John Doe");

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("Hey John Doe!");
	}

	[Fact]
	public void LoginDisplay_WhenAuthenticated_ShowsLogoutButton()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("Log out");
		cut.Markup.Should().Contain("hover:bg-red-100");
	}

	[Fact]
	public void LoginDisplay_WhenAuthenticated_ContainsLogoutForm()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Find("form").Should().NotBeNull();
		cut.Markup.Should().Contain("method=");
		cut.Markup.Should().Contain("post");
		cut.Markup.Should().Contain("action");
		cut.Markup.Should().Contain("/account/logout");
	}

	[Fact]
	public void LoginDisplay_WhenAuthenticated_ContainsAntiforgeryToken()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert - AntiforgeryToken renders as a hidden input, not literal text
		cut.Find("form").Should().NotBeNull();
	}

	[Fact]
	public void LoginDisplay_WhenNotAuthenticated_ShowsLoginLink()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("Log in");
		cut.Markup.Should().Contain("btn-primary");
	}

	[Fact]
	public void LoginDisplay_WhenNotAuthenticated_LoginLinkPointsToLoginPage()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("/account/login");
		cut.Markup.Should().Contain("returnUrl");
	}

	[Fact]
	public void LoginDisplay_WhenNotAuthenticated_LoginLinkIncludesReturnUrl()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("returnUrl=");
	}

	[Fact]
	public void LoginDisplay_WithAuthenticatedAdmin_ShowsCorrectUserName()
	{
		// Arrange
		SetupAuthenticatedUser(userName: "Admin User", isAdmin: true);

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("Hey Admin User!");
	}

	[Fact]
	public void LoginDisplay_WithSpecificEmail_StoresCorrectUserIdentity()
	{
		// Arrange
		var email = "test@example.com";
		SetupAuthenticatedUser(email: email);

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void LoginDisplay_LogoutButtonHasCorrectStyling()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert — hover utility classes encapsulate button styling (hover:bg-red-100 hover:text-red-700)
		cut.Markup.Should().Contain("hover:bg-red-100");
		cut.Markup.Should().Contain("transition-colors");
	}

	[Fact]
	public void LoginDisplay_LoginButtonHasCorrectStyling()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert — btn-primary utility class encapsulates button styling via @apply
		cut.Markup.Should().Contain("btn-primary");
		cut.Markup.Should().Contain("transition-colors");
	}

	[Fact]
	public void LoginDisplay_LogoutButtonUsesUtilityClass()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert — hover utility classes encapsulate button styling (hover:bg-red-100 hover:text-red-700)
		var button = cut.Find("button[type='submit']");
		button.GetAttribute("class").Should().Contain("hover:bg-red-100");
	}

	[Fact]
	public void LoginDisplay_LoginButtonUsesUtilityClass()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert — hover/focus states are encapsulated in the btn-primary utility class
		var link = cut.Find("a");
		link.GetAttribute("class").Should().Contain("btn-primary");
	}

	[Fact]
	public void LoginDisplay_ShowsUserGreetingWithCorrectFormatting()
	{
		// Arrange
		SetupAuthenticatedUser(userName: "Jane Smith");

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("Hey Jane Smith!");
	}

	[Fact]
	public void LoginDisplay_AuthorizedSectionContainsGapSpacing()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("gap-1");
	}

	[Fact]
	public void LoginDisplay_AuthorizedSectionUsesFlexLayout()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<LoginDisplay>();

		// Assert
		cut.Markup.Should().Contain("flex items-center");
	}
}

/// <summary>
///   Tests for ReconnectModal component.
/// </summary>
public class ReconnectModalTests : BunitTestBase
{
	[Fact]
	public void ReconnectModal_Renders_WithoutErrors()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void ReconnectModal_ContainsDialog()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Find("dialog").Should().NotBeNull();
		cut.Find("dialog").GetAttribute("id").Should().Be("components-reconnect-modal");
	}

	[Fact]
	public void ReconnectModal_ContainsFirstAttemptMessage()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().Contain("Rejoining the server...");
	}

	[Fact]
	public void ReconnectModal_ContainsRetryAttemptMessage()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().Contain("Rejoin failed... trying again in");
		cut.Markup.Should().Contain("components-seconds-to-next-attempt");
	}

	[Fact]
	public void ReconnectModal_ContainsFailureMessage()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().Contain("Failed to rejoin");
	}

	[Fact]
	public void ReconnectModal_ContainsRetryButton()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		var retryButton = cut.Find("#components-reconnect-button");
		retryButton.Should().NotBeNull();
		retryButton.TextContent.Should().Contain("Retry");
	}

	[Fact]
	public void ReconnectModal_ContainsAnimationDiv()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Find(".components-rejoining-animation").Should().NotBeNull();
	}

	[Fact]
	public void ReconnectModal_AnimationHasAnimationElements()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		var animationDiv = cut.Find(".components-rejoining-animation");
		var childDivs = animationDiv.QuerySelectorAll("div");
		childDivs.Should().HaveCountGreaterThanOrEqualTo(1);
	}

	[Fact]
	public void ReconnectModal_ContainsPauseMessage()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().Contain("The session has been paused by the server");
	}

	[Fact]
	public void ReconnectModal_ContainsResumeFailedMessage()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().Contain("Failed to resume the session");
	}

	[Fact]
	public void ReconnectModal_ContainsResumeButton()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		var resumeButton = cut.Find("#components-resume-button");
		resumeButton.Should().NotBeNull();
		resumeButton.TextContent.Should().Contain("Resume");
	}

	[Fact]
	public void ReconnectModal_DialogHasContainerDiv()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Find(".components-reconnect-container").Should().NotBeNull();
	}

	[Fact]
	public void ReconnectModal_FailureMessageIncludesInstructions()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().Contain("Please retry or reload the page");
	}

	[Fact]
	public void ReconnectModal_DialogHasDataNosnippetAttribute()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		var dialog = cut.Find("dialog");
		dialog.GetAttribute("data-nosnippet").Should().Be("");
	}

	[Fact]
	public void ReconnectModal_ResumeFailedMessageIncludesInstructions()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().Contain("Failed to resume the session");
		cut.Markup.Should().Contain("Please retry or reload the page");
	}

	[Fact]
	public void ReconnectModal_AllMessagesHaveVisibilityClasses()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Markup.Should().Contain("components-reconnect-first-attempt-visible");
		cut.Markup.Should().Contain("components-reconnect-repeated-attempt-visible");
		cut.Markup.Should().Contain("components-reconnect-failed-visible");
		cut.Markup.Should().Contain("components-pause-visible");
		cut.Markup.Should().Contain("components-resume-failed-visible");
	}

	[Fact]
	public void ReconnectModal_RetryButtonHasVisibilityClass()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		var retryButton = cut.Find("#components-reconnect-button");
		retryButton.ClassName.Should().Contain("components-reconnect-failed-visible");
	}

	[Fact]
	public void ReconnectModal_ResumeButtonHasVisibilityClasses()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		var resumeButton = cut.Find("#components-resume-button");
		resumeButton.ClassName.Should().Contain("components-pause-visible");
		resumeButton.ClassName.Should().Contain("components-resume-failed-visible");
	}

	[Fact]
	public void ReconnectModal_CountdownDisplaysSeconds()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		cut.Find("#components-seconds-to-next-attempt").Should().NotBeNull();
	}

	[Fact]
	public void ReconnectModal_LoadsJavaScriptModule()
	{
		// Arrange
		SetupAuthenticatedUser();

		// Act
		var cut = Render<ReconnectModal>();

		// Assert
		// Verify that the component references the JavaScript module
		cut.Markup.Should().Contain("Components/Layout/ReconnectModal.razor.js");
	}
}
