// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Categories.razor.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

using Radzen.Blazor;

namespace IssueTracker.UI.Pages;

/// <summary>
///   Categories partial class
/// </summary>
[UsedImplicitly]
public partial class Categories
{
	private List<CategoryModel>? _categories = new();

	private RadzenDataGrid<CategoryModel>? _categoriesGrid;
	private CategoryModel? _categoryToInsert;
	private CategoryModel? _categoryToUpdate;

	/// <summary>
	///   OnInitializedAsync event.
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_categories = await CategoryService.GetCategories();
	}

	private async Task EditRow(CategoryModel category)
	{
		_categoryToUpdate = category;

		await _categoriesGrid!.EditRow(_categoryToUpdate);
	}

	private async void OnUpdateRow(CategoryModel category)
	{
		_categoryToUpdate = null;

		await CategoryService.UpdateCategory(category);
	}

	private async Task SaveRow(CategoryModel category)
	{
		await _categoriesGrid!.UpdateRow(category);
	}

	private void CancelEdit(CategoryModel category)
	{
		if (category == _categoryToInsert)
		{
			_categoryToInsert = null;
		}

		if (category == _categoryToUpdate)
		{
			_categoryToUpdate = null;
		}

		_categoriesGrid!.CancelEditRow(category);
	}

	private async Task DeleteRow(CategoryModel category)
	{
		if (_categories!.Contains(category))
		{
			_categories.Remove(category);
		}

		_categoriesGrid!.CancelEditRow(category);

		await CategoryService.ArchiveCategory(category);

		await _categoriesGrid.Reload();
	}

	private async Task InsertRow()
	{
		_categoryToInsert = new CategoryModel();

		await _categoriesGrid!.InsertRow(_categoryToInsert);
	}

	private async void OnCreateRow(CategoryModel category)
	{
		if (category == _categoryToInsert)
		{
			_categoryToInsert = null;
		}

		await CategoryService.CreateCategory(category);

		_categories!.Add(category);

		await _categoriesGrid!.Reload();
	}

	/// <summary>
	///   ClosePage method.
	/// </summary>
	private void ClosePage()
	{
		NavManager.NavigateTo("/");
	}
}