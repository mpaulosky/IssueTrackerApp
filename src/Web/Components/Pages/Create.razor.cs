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
/// <seealso cref="Microsoft.AspNetCore.Components.ComponentBase" />
[UsedImplicitly]
public partial class Create : ComponentBase
{
	[Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private IIssueService IssueService { get; set; } = default!;
	[Inject] private ICategoryService CategoryService { get; set; } = default!;
	[Inject] private IStatusService StatusService { get; set; } = default!;
	[Inject] private IUserService UserService { get; set; } = default!;

	private List<Category>? _categories;
	private CreateIssueDto _issue = new();
	private global::Shared.Models.User? _loggedInUser;
	private List<global::Shared.Models.Status>? _statuses;

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
		ObjectId categoryId = ObjectId.Parse(_issue.CategoryId!);
		Category? category = _categories!.FirstOrDefault(c => c.Id == categoryId);
		global::Shared.Models.Status? status = _statuses!.FirstOrDefault(c => c.StatusName == "Watching");
		global::Shared.Models.Issue s = new()
		{
			Title = _issue.Title!,
			Description = _issue.Description!,
			Author = new UserDto(_loggedInUser!),
			Category = new CategoryDto(category!),
			IssueStatus = new StatusDto(status!)
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
