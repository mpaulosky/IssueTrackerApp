// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     SampleData.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace IssueTracker.UI.Pages;

/// <summary>
///   SampleData class
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Components.ComponentBase" />
[ExcludeFromCodeCoverage]
[UsedImplicitly]
public partial class SampleData : ComponentBase
{
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private IUserService UserService { get; set; } = default!;
	[Inject] private ICategoryService CategoryService { get; set; } = default!;
	[Inject] private IStatusService StatusService { get; set; } = default!;
	[Inject] private ICommentService CommentService { get; set; } = default!;
	[Inject] private IIssueService IssueService { get; set; } = default!;

	private bool _categoriesCreated;
	private bool _commentsCreated;
	private bool _issuesCreated;
	private bool _statusesCreated;
	private bool _usersCreated;

	protected override async Task OnInitializedAsync()
	{
		await SetButtonStatus();
	}

	private async Task SetButtonStatus()
	{
		_usersCreated = (await UserService.GetUsers()).Any();
		_categoriesCreated = (await CategoryService.GetCategories()).Any();
		_statusesCreated = (await StatusService.GetStatuses()).Any();
		_commentsCreated = (await CommentService.GetComments()).Any();
		_issuesCreated = (await IssueService.GetIssues()).Any();
	}

	/// <summary>
	///   Creates the Users method.
	/// </summary>
	private async Task CreateUsers()
	{
		List<global::Shared.Models.User> users = await UserService.GetUsers();

		if (users.Count > 0)
		{
			return;
		}

		IEnumerable<global::Shared.Models.User> items = FakeUser.GetUsers(2);

		foreach (global::Shared.Models.User? item in items)
		{
			await UserService.CreateUser(item);
		}

		_usersCreated = true;
	}

	/// <summary>
	///   Creates the categories method.
	/// </summary>
	private async Task CreateCategories()
	{
		List<Category> categories = await CategoryService.GetCategories();

		if (categories.Count > 0)
		{
			return;
		}

		Category item = new() { CategoryName = "Design", CategoryDescription = "An Issue with the design." };
		await CategoryService.CreateCategory(item);

		item = new Category
		{
			CategoryName = "Documentation",
			CategoryDescription = "An Issue with the documentation."
		};
		await CategoryService.CreateCategory(item);

		item = new Category
		{
			CategoryName = "Implementation",
			CategoryDescription = "An Issue with the implementation."
		};
		await CategoryService.CreateCategory(item);

		item = new Category
		{
			CategoryName = "Clarification",
			CategoryDescription = "A quick Issue with a general question."
		};
		await CategoryService.CreateCategory(item);

		item = new Category { CategoryName = "Miscellaneous", CategoryDescription = "Not sure where this fits." };
		await CategoryService.CreateCategory(item);

		_categoriesCreated = true;
	}

	/// <summary>
	///   Creates the statuses method.
	/// </summary>
	private async Task CreateStatuses()
	{
		List<global::Shared.Models.Status> statuses = await StatusService.GetStatuses();

		if (statuses.Count > 0)
		{
			return;
		}

		global::Shared.Models.Status item = new()
		{
			StatusName = "Answered",
			StatusDescription = "The suggestion was accepted and the corresponding item was created."
		};
		await StatusService.CreateStatus(item);

		item = new global::Shared.Models.Status
		{
			StatusName = "Watching",
			StatusDescription =
				"The suggestion is interesting. We are watching to see how much interest there is in it."
		};
		await StatusService.CreateStatus(item);

		item = new global::Shared.Models.Status
		{
			StatusName = "Upcoming",
			StatusDescription = "The suggestion was accepted and it will be released soon."
		};
		await StatusService.CreateStatus(item);

		item = new global::Shared.Models.Status
		{
			StatusName = "Dismissed",
			StatusDescription = "The suggestion was not something that we are going to undertake."
		};
		await StatusService.CreateStatus(item);

		_statusesCreated = true;
	}

	/// <summary>
	///   Creates the comments method.
	/// </summary>
	private async Task CreateComments()
	{
		List<Shared.Models.Comment> comments = await CommentService.GetComments();

		if (comments.Count > 0)
		{
			return;
		}

		IEnumerable<Shared.Models.Comment> items = FakeComment.GetComments(4);

		foreach (Shared.Models.Comment? item in items)
		{
			await CommentService.CreateComment(item);
		}

		_commentsCreated = true;
	}

	/// <summary>
	///   Creates Issues method.
	/// </summary>
	private async Task CreateIssues()
	{
		List<global::Shared.Models.Issue> issues = await IssueService.GetIssues();

		if (issues.Count > 0)
		{
			return;
		}

		IEnumerable<global::Shared.Models.Issue> items = FakeIssue.GetIssues(6);

		foreach (global::Shared.Models.Issue? issue in items)
		{
			await IssueService.CreateIssue(issue);
		}

		_issuesCreated = true;
	}
}
