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
public partial class SampleData
{
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
		List<UserModel> users = await UserService.GetUsers();

		if (users.Count > 0)
		{
			return;
		}

		IEnumerable<UserModel> items = FakeUser.GetUsers(2);

		foreach (UserModel? item in items)
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
		List<CategoryModel> categories = await CategoryService.GetCategories();

		if (categories.Count > 0)
		{
			return;
		}

		CategoryModel item = new() { CategoryName = "Design", CategoryDescription = "An Issue with the design." };
		await CategoryService.CreateCategory(item);

		item = new CategoryModel
		{
			CategoryName = "Documentation", CategoryDescription = "An Issue with the documentation."
		};
		await CategoryService.CreateCategory(item);

		item = new CategoryModel
		{
			CategoryName = "Implementation", CategoryDescription = "An Issue with the implementation."
		};
		await CategoryService.CreateCategory(item);

		item = new CategoryModel
		{
			CategoryName = "Clarification", CategoryDescription = "A quick Issue with a general question."
		};
		await CategoryService.CreateCategory(item);

		item = new CategoryModel { CategoryName = "Miscellaneous", CategoryDescription = "Not sure where this fits." };
		await CategoryService.CreateCategory(item);

		_categoriesCreated = true;
	}

	/// <summary>
	///   Creates the statuses method.
	/// </summary>
	private async Task CreateStatuses()
	{
		List<StatusModel> statuses = await StatusService.GetStatuses();

		if (statuses.Count > 0)
		{
			return;
		}

		StatusModel item = new()
		{
			StatusName = "Answered",
			StatusDescription = "The suggestion was accepted and the corresponding item was created."
		};
		await StatusService.CreateStatus(item);

		item = new StatusModel
		{
			StatusName = "Watching",
			StatusDescription =
				"The suggestion is interesting. We are watching to see how much interest there is in it."
		};
		await StatusService.CreateStatus(item);

		item = new StatusModel
		{
			StatusName = "Upcoming", StatusDescription = "The suggestion was accepted and it will be released soon."
		};
		await StatusService.CreateStatus(item);

		item = new StatusModel
		{
			StatusName = "Dismissed", StatusDescription = "The suggestion was not something that we are going to undertake."
		};
		await StatusService.CreateStatus(item);

		_statusesCreated = true;
	}

	/// <summary>
	///   Creates the comments method.
	/// </summary>
	private async Task CreateComments()
	{
		List<CommentModel> comments = await CommentService.GetComments();

		if (comments.Count > 0)
		{
			return;
		}

		IEnumerable<CommentModel> items = FakeComment.GetComments(4);

		foreach (CommentModel? item in items)
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
		List<IssueModel> issues = await IssueService.GetIssues();

		if (issues.Count > 0)
		{
			return;
		}

		IEnumerable<IssueModel> items = FakeIssue.GetIssues(6);

		foreach (IssueModel? issue in items)
		{
			await IssueService.CreateIssue(issue);
		}

		_issuesCreated = true;
	}
}