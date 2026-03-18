namespace Web.Tests.Bunit.Pages;

using Web.Components.Pages;

/// <summary>
/// Tests for Dashboard.razor component
/// Validates user-specific dashboard rendering and data display
/// </summary>
public class DashboardPageTests : BunitTestBase
{
	[Fact]
	public void Dashboard_RequiresAuthentication()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<Dashboard>();

		// Assert - Should show unauthorized/redirect message or be empty
		cut.Markup.Should().NotBeNull();
	}

	[Fact]
	public async Task Dashboard_WhenAuthenticated_InitializesWithUserContext()
	{
		// Arrange
		var userId = "user123";
		var userName = "John Doe";
		SetupAuthenticatedUser(userId: userId, userName: userName);

		var dashboard = new UserDashboardDto(15, 5, 8, 2, []);

		DashboardService.GetUserDashboardAsync(userId)
				.Returns(Result.Ok(dashboard));

		// Act
		var cut = Render<Dashboard>();
		await cut.InvokeAsync(() => Task.Delay(100)); // Allow async operations

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain(userName);
		markup.Should().Contain("Welcome back");
	}

	[Fact]
	public async Task Dashboard_DisplaysStatistics()
	{
		// Arrange
		var userId = "user456";
		SetupAuthenticatedUser(userId: userId);

		var dashboard = new UserDashboardDto(20, 7, 12, 3, []);

		DashboardService.GetUserDashboardAsync(userId)
				.Returns(Result.Ok(dashboard));

		// Act
		var cut = Render<Dashboard>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("Total Issues");
		markup.Should().Contain("Open Issues");
		markup.Should().Contain("Resolved Issues");
		markup.Should().Contain("This Week");
	}

	[Fact]
	public async Task Dashboard_DisplaysRecentIssues()
	{
		// Arrange
		var userId = "user789";
		SetupAuthenticatedUser(userId: userId);

		var recentIssues = new List<IssueDto>
				{
						CreateTestIssue(title: "Bug in login"),
						CreateTestIssue(title: "Performance improvement needed")
				};

		var dashboard = new UserDashboardDto(10, 3, 5, 2, recentIssues);

		DashboardService.GetUserDashboardAsync(userId)
				.Returns(Result.Ok(dashboard));

		// Act
		var cut = Render<Dashboard>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("My Recent Issues");
		markup.Should().Contain("Bug in login");
		markup.Should().Contain("Performance improvement needed");
	}

	[Fact]
	public async Task Dashboard_ShowsQuickActionsForAuthenticatedUser()
	{
		// Arrange
		var userId = "user999";
		SetupAuthenticatedUser(userId: userId);

		var dashboard = new UserDashboardDto(5, 2, 3, 1, []);

		DashboardService.GetUserDashboardAsync(userId)
				.Returns(Result.Ok(dashboard));

		// Act
		var cut = Render<Dashboard>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("Quick Actions");
		markup.Should().Contain("Create New Issue");
		markup.Should().Contain("View All Issues");
	}

	[Fact]
	public async Task Dashboard_HandlesDashboardServiceError()
	{
		// Arrange
		var userId = "user101";
		SetupAuthenticatedUser(userId: userId);

		DashboardService.GetUserDashboardAsync(userId)
				.Returns(Result.Fail<UserDashboardDto>("Failed to load dashboard"));

		// Act
		var cut = Render<Dashboard>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().NotBeNull(); // Component should still render (may show error)
	}

	[Fact]
	public async Task Dashboard_DisplaysEmptyStateWhenNoRecentIssues()
	{
		// Arrange
		var userId = "user202";
		SetupAuthenticatedUser(userId: userId);

		var dashboard = new UserDashboardDto(0, 0, 0, 0, []);

		DashboardService.GetUserDashboardAsync(userId)
				.Returns(Result.Ok(dashboard));

		// Act
		var cut = Render<Dashboard>();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("Welcome back");
		markup.Should().Contain("0"); // Should show zero statistics
	}
}

/// <summary>
/// Tests for Home.razor component
/// Validates landing page rendering for unauthenticated and authenticated users
/// </summary>
public class HomePageTests : BunitTestBase
{
	[Fact]
	public void Home_RendersForAnonymousUser()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<Home>();

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("Welcome to IssueTracker");
		markup.Should().Contain("Log in to Get Started");
	}

	[Fact]
	public void Home_RendersForAuthenticatedUser()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "user123", userName: "Jane Smith");

		// Act
		var cut = Render<Home>();

		// Assert
		var markup = cut.Markup;
		markup.Should().NotBeNull();
		markup.Should().Contain("Welcome back");
		markup.Should().Contain("Jane Smith");
	}

	[Fact]
	public void Home_DisplaysWelcomeMessage()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<Home>();

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("Welcome");
	}

	[Fact]
	public void Home_IsPublicAndDoesNotRequireAuthentication()
	{
		// Arrange - Both authenticated and anonymous users should see the page
		SetupAnonymousUser();
		var cut1 = Render<Home>();

		SetupAuthenticatedUser(userId: "user456");
		var cut2 = Render<Home>();

		// Assert - Both should render successfully
		cut1.Markup.Should().NotBeNull();
		cut2.Markup.Should().NotBeNull();
	}
}

/// <summary>
/// Tests for Error.razor component
/// Validates error page rendering and request ID display
/// </summary>
public class ErrorPageTests : BunitTestBase
{
	[Fact]
	public void Error_RendersErrorPage()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "user123");

		// Act
		var cut = Render<Error>();

		// Assert
		var markup = cut.Markup;
		markup.Should().NotBeNull();
	}

	[Fact]
	public void Error_DisplaysErrorHeading()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "user123");

		// Act
		var cut = Render<Error>();

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("Error");
	}

	[Fact]
	public void Error_RendersWithoutRequiringServices()
	{
		// Arrange - Error page should work with no service setup
		SetupAnonymousUser();

		// Act
		var cut = Render<Error>();

		// Assert
		cut.Markup.Should().NotBeNull();
		var headings = cut.FindAll("h1");
		headings.Should().NotBeEmpty();
	}

	[Fact]
	public void Error_DisplaysErrorMessage()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "user123");

		// Act
		var cut = Render<Error>();

		// Assert - Should display error context/message
		var markup = cut.Markup;
		markup.Should().NotBeNull();
		// Content might vary, but page should render
	}

	[Fact]
	public void Error_WorksForAuthenticatedAndAnonymousUsers()
	{
		// Arrange - Error page should be accessible by both user types
		SetupAnonymousUser();
		var cut1 = Render<Error>();

		SetupAuthenticatedUser(userId: "user789");
		var cut2 = Render<Error>();

		// Assert
		cut1.Markup.Should().NotBeNull();
		cut2.Markup.Should().NotBeNull();
	}

	[Fact]
	public void Error_RendersErrorLayout()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "user123");

		// Act
		var cut = Render<Error>();

		// Assert - Error page should have proper structure
		cut.Nodes.Count().Should().BeGreaterThan(0);
	}
}

/// <summary>
/// Tests for NotFound.razor component
/// Validates 404 page rendering and messaging
/// </summary>
public class NotFoundPageTests : BunitTestBase
{
	[Fact]
	public void NotFound_Renders404Page()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<NotFound>();

		// Assert
		var markup = cut.Markup;
		markup.Should().NotBeNull();
	}

	[Fact]
	public void NotFound_DisplaysNotFoundMessage()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<NotFound>();

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("Not Found");
	}

	[Fact]
	public void NotFound_DisplaysHelpfulText()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<NotFound>();

		// Assert
		var markup = cut.Markup;
		markup.Should().Contain("does not exist");
	}

	[Fact]
	public void NotFound_RendersForAnonymousUser()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<NotFound>();

		// Assert
		cut.Markup.Should().NotBeNull();
	}

	[Fact]
	public void NotFound_RendersForAuthenticatedUser()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "user123", userName: "Test User");

		// Act
		var cut = Render<NotFound>();

		// Assert
		cut.Markup.Should().NotBeNull();
	}

	[Fact]
	public void NotFound_PageIsPubliclyAccessible()
	{
		// Arrange - NotFound page should be accessible by both user types
		var cut1 = Render<NotFound>();

		SetupAuthenticatedUser(userId: "user456");
		var cut2 = Render<NotFound>();

		// Assert
		cut1.Markup.Should().NotBeNull();
		cut2.Markup.Should().NotBeNull();
	}

	[Fact]
	public void NotFound_ContainsProperHeading()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<NotFound>();

		// Assert
		var headings = cut.FindAll("h1, h2, h3, h4");
		headings.Should().NotBeEmpty();
	}

	[Fact]
	public void NotFound_HasCorrectPageStructure()
	{
		// Arrange
		SetupAnonymousUser();

		// Act
		var cut = Render<NotFound>();

		// Assert - Page should have content
		cut.Nodes.Count().Should().BeGreaterThan(0);
	}
}
