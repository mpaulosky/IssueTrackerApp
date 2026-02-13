// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Index.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace IssueTracker.UI.Pages;

/// <summary>
///   Index page class
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Mvc.RazorPages.PageModel" />
[UsedImplicitly]
public partial class Index
{
	private List<CategoryModel>? _categories;
	private bool _isSortedByNew = true;
	private List<IssueModel>? _issues = new();

	private UserModel? _loggedInUser;
	private string? _searchText = string.Empty;
	private string? _selectedCategory = "All";
	private string? _selectedStatus = "All";
	private bool _showCategories;
	private bool _showStatuses;
	private List<StatusModel>? _statuses;

	/// <summary>
	///   OnInitializedAsync event
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_categories = await CategoryService.GetCategories();
		_statuses = await StatusService.GetStatuses();
		await LoadAndVerifyUser();
	}

	/// <summary>
	///   LoadCreateIssuePage method
	/// </summary>
	private void LoadCreateIssuePage()
	{
		if (_loggedInUser is not null)
		{
			NavManager.NavigateTo("/Create");
		}
		else
		{
			NavManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
		}
	}

	/// <summary>
	///   LoadAndVerifyUser method
	/// </summary>
	private async Task LoadAndVerifyUser()
	{
		AuthenticationState authState = await AuthProvider.GetAuthenticationStateAsync();
		string? objectId = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("objectidentifier"))?.Value;
		if (!string.IsNullOrWhiteSpace(objectId))
		{
			_loggedInUser = await UserService.GetUserFromAuthentication(objectId);
			string? firstName = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("givenname"))?.Value;
			string? lastName = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("surname"))?.Value;
			string? displayName = authState.User.Claims.FirstOrDefault(c => c.Type.Equals("name"))?.Value;
			string? email = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value;
			bool isDirty = false;

			if (!objectId.Equals(_loggedInUser.ObjectIdentifier))
			{
				isDirty = true;
				_loggedInUser.ObjectIdentifier = objectId;
			}

			if (firstName?.Equals(_loggedInUser.FirstName) == false)
			{
				isDirty = true;
				_loggedInUser.FirstName = firstName;
			}

			if (lastName?.Equals(_loggedInUser.LastName) == false)
			{
				isDirty = true;
				_loggedInUser.LastName = lastName;
			}

			if (displayName?.Equals(_loggedInUser.DisplayName) == false)
			{
				isDirty = true;
				_loggedInUser.DisplayName = displayName;
			}

			if (email?.Equals(_loggedInUser.EmailAddress) == false)
			{
				isDirty = true;
				_loggedInUser.EmailAddress = email;
			}

			if (isDirty)
			{
				if (string.IsNullOrWhiteSpace(_loggedInUser.Id))
				{
					await UserService.CreateUser(_loggedInUser);
				}
				else
				{
					await UserService.UpdateUser(_loggedInUser);
				}
			}
		}
	}

	/// <summary>
	///   OnAfterRenderAsync event
	/// </summary>
	/// <param name="firstRender">bool</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await LoadFilterState();
			await FilterIssues();
			StateHasChanged();
		}
	}

	/// <summary>
	///   LoadFilterState method
	/// </summary>
	private async Task LoadFilterState()
	{
		if (SessionStorage is not null)
		{
			string? stringResults = await SessionStorage.GetItemAsync<string>(nameof(_selectedCategory));

			_selectedCategory = string.IsNullOrWhiteSpace(stringResults) ? "All" : stringResults;

			stringResults = await SessionStorage.GetItemAsync<string>(nameof(_selectedStatus));

			_selectedStatus = string.IsNullOrWhiteSpace(stringResults) ? "All" : stringResults;

			stringResults = await SessionStorage.GetItemAsync<string>(nameof(_searchText));

			_searchText = string.IsNullOrWhiteSpace(stringResults) ? string.Empty : stringResults;

			bool boolResults = await SessionStorage.GetItemAsync<bool>(nameof(_isSortedByNew));

			_isSortedByNew = boolResults;
		}
	}

	/// <summary>
	///   SaveFilterState method
	/// </summary>
	private async Task SaveFilterState()
	{
		await SessionStorage.SetItemAsync(nameof(_selectedCategory), _selectedCategory);
		await SessionStorage.SetItemAsync(nameof(_selectedStatus), _selectedStatus);
		await SessionStorage.SetItemAsync(nameof(_searchText), _searchText);
		await SessionStorage.SetItemAsync(nameof(_isSortedByNew), _isSortedByNew);
	}

	/// <summary>
	///   FilterIssues method
	/// </summary>
	private async Task FilterIssues()
	{
		List<IssueModel> output = await IssueService.GetApprovedIssues();

		if (_selectedCategory != "All")
		{
			output = output.Where(s => s.Category.CategoryName == _selectedCategory).ToList();
		}

		if (_selectedStatus != "All")
		{
			output = output.Where(s => s.IssueStatus.StatusName == _selectedStatus).ToList();
		}

		if (!string.IsNullOrWhiteSpace(_searchText))
		{
			output = output.Where(s =>
					s.Title.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase) ||
					s.Description.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase))
				.ToList();
		}

		if (_isSortedByNew)
		{
			output = output.OrderByDescending(s => s.DateCreated).ToList();
		}

		_issues = output;

		await SaveFilterState();
	}

	/// <summary>
	///   OrderByNew method
	/// </summary>
	/// <param name="isNew">bool</param>
	private async Task OrderByNew(bool isNew)
	{
		_isSortedByNew = isNew;
		await FilterIssues();
	}

	/// <summary>
	///   OnSearchInput method
	/// </summary>
	/// <param name="searchInput">string</param>
	private async Task OnSearchInput(string searchInput)
	{
		_searchText = searchInput;
		await FilterIssues();
	}

	/// <summary>
	///   OnCategoryClick method
	/// </summary>
	/// <param name="category">string</param>
	private async Task OnCategoryClick(string category = "All")
	{
		_selectedCategory = category;
		_showCategories = false;
		await FilterIssues();
	}

	/// <summary>
	///   OnStatusClick method
	/// </summary>
	/// <param name="status">string</param>
	private async Task OnStatusClick(string status = "All")
	{
		_selectedStatus = status;
		_showStatuses = false;
		await FilterIssues();
	}

	/// <summary>
	///   SortedByNewCssClass method
	/// </summary>
	/// <param name="isNew">bool</param>
	/// <returns>string</returns>
	private string SortedByNewCssClass(bool isNew)
	{
		return isNew == _isSortedByNew ? "sort-selected" : string.Empty;
	}

	/// <summary>
	///   GetSelectedCategoryCssClass method
	/// </summary>
	/// <param name="category">string</param>
	/// <returns>string</returns>
	private string GetSelectedCategoryCssClass(string category = "All")
	{
		return category == _selectedCategory ? "selected-category" : string.Empty;
	}

	/// <summary>
	///   GetSelectedStatusCssClass method
	/// </summary>
	/// <param name="status">string</param>
	/// <returns>string</returns>
	private string GetSelectedStatusCssClass(string status = "All")
	{
		return status == _selectedStatus ? "selected-status" : string.Empty;
	}
}