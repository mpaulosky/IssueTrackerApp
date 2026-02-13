// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Create.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace IssueTracker.UI.Pages;

/// <summary>
///   Create class
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Mvc.RazorPages.PageModel" />
[UsedImplicitly]
public partial class Create
{
	private List<CategoryModel>? _categories;
	private CreateIssueDto _issue = new();
	private UserModel? _loggedInUser;
	private List<StatusModel>? _statuses;

	/// <summary>
	///   OnInitializedAsync method
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_loggedInUser = await AuthProvider.GetUserFromAuth(UserService);
		_categories = await CategoryService.GetCategories();
		_statuses = await StatusService.GetStatuses();
	}

	/// <summary>
	///   CreateIssue method
	/// </summary>
	private async Task CreateIssue()
	{
		CategoryModel? category = _categories!.FirstOrDefault(c => c.Id == _issue.CategoryId);
		StatusModel? status = _statuses!.FirstOrDefault(c => c.StatusName == "Watching");
		IssueModel s = new()
		{
			Title = _issue.Title!,
			Description = _issue.Description!,
			Author = new BasicUserModel(_loggedInUser!),
			Category = new BasicCategoryModel(category!),
			IssueStatus = new BasicStatusModel(status!)
		};

		await IssueService.CreateIssue(s);

		_issue = new CreateIssueDto();
		ClosePage();
	}

	/// <summary>
	///   ClosePage method
	/// </summary>
	private void ClosePage()
	{
		NavManager.NavigateTo("/");
	}
}