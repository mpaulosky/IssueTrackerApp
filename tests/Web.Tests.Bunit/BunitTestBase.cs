// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BunitTestBase.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using Web.Services;

namespace Web.Tests.Bunit;

/// <summary>
///   Base class for bUnit component tests providing common test infrastructure.
/// </summary>
public abstract class BunitTestBase : BunitContext
{
	protected IMediator Mediator { get; }
	protected IIssueService IssueService { get; }
	protected ICommentService CommentService { get; }
	protected IAnalyticsService AnalyticsService { get; }
	protected IAttachmentService AttachmentService { get; }
	protected IBulkOperationService BulkOperationService { get; }
	protected INotificationService NotificationService { get; }
	protected ICategoryService CategoryService { get; }
	protected IStatusService StatusService { get; }
	protected IJSRuntime JsRuntime { get; }
protected IDashboardService DashboardService { get; }

	protected BunitTestBase()
	{
		// Create mocks
		Mediator = Substitute.For<IMediator>();
		IssueService = Substitute.For<IIssueService>();
		CommentService = Substitute.For<ICommentService>();
		AnalyticsService = Substitute.For<IAnalyticsService>();
		AttachmentService = Substitute.For<IAttachmentService>();
		BulkOperationService = Substitute.For<IBulkOperationService>();
		NotificationService = Substitute.For<INotificationService>();
		CategoryService = Substitute.For<ICategoryService>();
		StatusService = Substitute.For<IStatusService>();
		JsRuntime = Substitute.For<IJSRuntime>();
DashboardService = Substitute.For<IDashboardService>();

		// Register mocks
		Services.AddSingleton(Mediator);
		Services.AddSingleton(IssueService);
		Services.AddSingleton(CommentService);
		Services.AddSingleton(AnalyticsService);
		Services.AddSingleton(AttachmentService);
		Services.AddSingleton(BulkOperationService);
		Services.AddSingleton(NotificationService);
		Services.AddSingleton(CategoryService);
		Services.AddSingleton(StatusService);
		Services.AddSingleton(JsRuntime);
Services.AddSingleton(DashboardService);

		// Add fake navigation manager
		Services.AddSingleton<NavigationManager>(new FakeNavigationManager(this));

		// Add authorization state
		SetupAuthenticatedUser();
	}

	/// <summary>
	///   Sets up an authenticated test user.
	/// </summary>
	protected void SetupAuthenticatedUser(string userId = "test-user-id", string userName = "Test User",
		string email = "test@example.com", bool isAdmin = false)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, userId),
			new(ClaimTypes.Name, userName),
			new(ClaimTypes.Email, email)
		};

		if (isAdmin)
		{
			claims.Add(new Claim("role", "admin"));
		}

		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);

		var authState = Task.FromResult(new AuthenticationState(principal));
		Services.AddSingleton<AuthenticationStateProvider>(
			new TestAuthStateProvider(authState));
		Services.AddSingleton(sp => sp.GetRequiredService<AuthenticationStateProvider>());
		Services.AddCascadingValue(sp => authState);
	}

	/// <summary>
	///   Sets up an anonymous (not authenticated) user.
	/// </summary>
	protected void SetupAnonymousUser()
	{
		var identity = new ClaimsIdentity();
		var principal = new ClaimsPrincipal(identity);
		var authState = Task.FromResult(new AuthenticationState(principal));

		Services.AddSingleton<AuthenticationStateProvider>(
			new TestAuthStateProvider(authState));
		Services.AddCascadingValue(sp => authState);
	}

	/// <summary>
	///   Creates a test UserDto.
	/// </summary>
	protected static UserDto CreateTestUser(string? id = null, string? name = null, string? email = null)
	{
		return new UserDto(
			id ?? "test-user-id",
			name ?? "Test User",
			email ?? "test@example.com"
		);
	}

	/// <summary>
	///   Creates a test CategoryDto.
	/// </summary>
	protected static CategoryDto CreateTestCategory(string? id = null, string? name = null)
	{
		return new CategoryDto(
			Id: id is not null ? MongoDB.Bson.ObjectId.Parse(id) : MongoDB.Bson.ObjectId.GenerateNewId(),
			CategoryName: name ?? "Test Category",
			CategoryDescription: "Test category description",
			DateCreated: DateTime.UtcNow,
			DateModified: null,
			Archived: false,
			ArchivedBy: UserDto.Empty
		);
	}

	/// <summary>
	///   Creates a test StatusDto.
	/// </summary>
	protected static StatusDto CreateTestStatus(string? id = null, string? name = null)
	{
		return new StatusDto(
			Id: id is not null ? MongoDB.Bson.ObjectId.Parse(id) : MongoDB.Bson.ObjectId.GenerateNewId(),
			StatusName: name ?? "Open",
			StatusDescription: "Test status description",
			DateCreated: DateTime.UtcNow,
			DateModified: null,
			Archived: false,
			ArchivedBy: UserDto.Empty
		);
	}

	/// <summary>
	///   Creates a test IssueDto.
	/// </summary>
	protected static IssueDto CreateTestIssue(
		string? id = null,
		string? title = null,
		string? description = null,
		UserDto? author = null,
		CategoryDto? category = null,
		StatusDto? status = null)
	{
		return new IssueDto(
			Id: id is not null ? MongoDB.Bson.ObjectId.Parse(id) : MongoDB.Bson.ObjectId.GenerateNewId(),
			Title: title ?? "Test Issue",
			Description: description ?? "Test Description",
			DateCreated: DateTime.UtcNow,
			DateModified: null,
			Author: author ?? CreateTestUser(),
			Category: category ?? CreateTestCategory(),
			Status: status ?? CreateTestStatus(),
			Archived: false,
			ArchivedBy: UserDto.Empty,
			ApprovedForRelease: false,
			Rejected: false
		);
	}

	/// <summary>
	///   Creates a test CommentDto.
	/// </summary>
	protected static CommentDto CreateTestComment(
		string? id = null,
		string? title = null,
		string? description = null,
		UserDto? author = null,
		IssueDto? issue = null)
	{
		return new CommentDto(
			Id: id is not null ? MongoDB.Bson.ObjectId.Parse(id) : MongoDB.Bson.ObjectId.GenerateNewId(),
			Title: title ?? "Test Comment",
			Description: description ?? "Test comment content",
			DateCreated: DateTime.UtcNow,
			DateModified: null,
			Issue: issue ?? CreateTestIssue(),
			Author: author ?? CreateTestUser(),
			UserVotes: [],
			Archived: false,
			ArchivedBy: UserDto.Empty,
			IsAnswer: false,
			AnswerSelectedBy: UserDto.Empty
		);
	}

	/// <summary>
	///   Creates a list of test issues for pagination testing.
	/// </summary>
	protected static List<IssueDto> CreateTestIssues(int count)
	{
		return Enumerable.Range(1, count)
			.Select(i => CreateTestIssue(
				title: $"Test Issue {i}",
				description: $"Description for issue {i}"
			))
			.ToList();
	}
}

/// <summary>
///   Test authentication state provider.
/// </summary>
internal class TestAuthStateProvider : AuthenticationStateProvider
{
	private readonly Task<AuthenticationState> _authState;

	public TestAuthStateProvider(Task<AuthenticationState> authState)
	{
		_authState = authState;
	}

	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		return _authState;
	}
}

/// <summary>
///   Fake NavigationManager for testing.
/// </summary>
internal class FakeNavigationManager : NavigationManager
{
	private readonly BunitContext _context;

	public FakeNavigationManager(BunitContext context)
	{
		_context = context;
		Initialize("http://localhost/", "http://localhost/");
	}

	protected override void NavigateToCore(string uri, bool forceLoad)
	{
		Uri = ToAbsoluteUri(uri).ToString();
	}
}
