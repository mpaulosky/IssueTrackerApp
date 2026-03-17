// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BunitTestBase.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using System.Security.Claims;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Web.Auth;

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
	protected IDashboardService DashboardService { get; }
	protected ILookupService LookupService { get; }

	private readonly BunitAuthorizationContext _authContext;

	protected BunitTestBase()
	{
		// Use bUnit's built-in JSInterop in Loose mode so unmocked JS calls
		// return defaults instead of throwing or hanging.
		JSInterop.Mode = JSRuntimeMode.Loose;

		// Set up bUnit's fake authorization (handles AuthorizeView, policies, etc.)
		_authContext = this.AddAuthorization();

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
		DashboardService = Substitute.For<IDashboardService>();
		LookupService = Substitute.For<ILookupService>();

		// Register interface mocks
		Services.AddSingleton(Mediator);
		Services.AddSingleton(IssueService);
		Services.AddSingleton(CommentService);
		Services.AddSingleton(AnalyticsService);
		Services.AddSingleton(AttachmentService);
		Services.AddSingleton(BulkOperationService);
		Services.AddSingleton(NotificationService);
		Services.AddSingleton(CategoryService);
		Services.AddSingleton(StatusService);
		Services.AddSingleton(DashboardService);
		Services.AddSingleton(LookupService);

		// Register concrete services required by page components
		var toastService = new ToastService();
		var fakeNav = new FakeNavigationManager();
		Services.AddSingleton(toastService);
		Services.AddSingleton(new BulkSelectionState());
		Services.AddSingleton(new SignalRClientService(
			NullLogger<SignalRClientService>.Instance,
			toastService,
			fakeNav));

		// Set up sensible default returns for common services so that
		// unmocked calls return empty successful results instead of null
		// (NSubstitute returns null for Task<T> by default which causes NRE).
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(Enumerable.Empty<StatusDto>())));
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(Enumerable.Empty<CategoryDto>())));
		AttachmentService.GetIssueAttachmentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<AttachmentDto>>(Array.Empty<AttachmentDto>())));
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));
		IssueService.SearchIssuesAsync(Arg.Any<IssueSearchRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(PagedResult<IssueDto>.Empty)));
		DashboardService.GetUserDashboardAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(new UserDashboardDto(0, 0, 0, 0, []))));
		AnalyticsService.GetAnalyticsSummaryAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<AnalyticsSummaryDto>("No data")));

		// Register logging infrastructure
		Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
		Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

		// Add fake navigation manager
		Services.AddSingleton<NavigationManager>(fakeNav);

		// Set default authenticated user
		SetupAuthenticatedUser();
	}

	/// <summary>
	///   Sets up an authenticated test user with bUnit's fake authorization.
	/// </summary>
	protected void SetupAuthenticatedUser(string userId = "test-user-id", string userName = "Test User",
		string email = "test@example.com", bool isAdmin = false)
	{
		_authContext.SetAuthorized(userName);
		_authContext.SetClaims(
			new Claim(ClaimTypes.NameIdentifier, userId),
			new Claim(ClaimTypes.Name, userName),
			new Claim(ClaimTypes.Email, email),
			new Claim(ClaimTypes.Role, AuthorizationRoles.User)
		);
		_authContext.SetPolicies(AuthorizationPolicies.UserPolicy);

		if (isAdmin)
		{
			_authContext.SetClaims(
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Name, userName),
				new Claim(ClaimTypes.Email, email),
				new Claim(ClaimTypes.Role, AuthorizationRoles.User),
				new Claim(ClaimTypes.Role, AuthorizationRoles.Admin)
			);
			_authContext.SetRoles(AuthorizationRoles.User, AuthorizationRoles.Admin);
			_authContext.SetPolicies(AuthorizationPolicies.UserPolicy, AuthorizationPolicies.AdminPolicy);
		}
	}

	/// <summary>
	///   Sets up an anonymous (not authenticated) user.
	/// </summary>
	protected void SetupAnonymousUser()
	{
		_authContext.SetNotAuthorized();
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
		MongoDB.Bson.ObjectId? issueId = null)
	{
		return new CommentDto(
			Id: id is not null ? MongoDB.Bson.ObjectId.Parse(id) : MongoDB.Bson.ObjectId.GenerateNewId(),
			Title: title ?? "Test Comment",
			Description: description ?? "Test comment content",
			DateCreated: DateTime.UtcNow,
			DateModified: null,
			IssueId: issueId ?? MongoDB.Bson.ObjectId.GenerateNewId(),
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
///   Fake NavigationManager for testing.
/// </summary>
internal class FakeNavigationManager : NavigationManager
{
	public FakeNavigationManager()
	{
		Initialize("http://localhost/", "http://localhost/");
	}

	protected override void NavigateToCore(string uri, bool forceLoad)
	{
		Uri = ToAbsoluteUri(uri).ToString();
	}
}
