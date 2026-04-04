// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SearchInputTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Shared;

namespace Web.Tests.Bunit.Components.Shared;

/// <summary>
///   Tests for the SearchInput component.
/// </summary>
public sealed class SearchInputTests : BunitTestBase
{
	#region Render Tests

	[Fact]
	public void Renders_TextInput()
	{
		// Arrange & Act
		var cut = Render<SearchInput>();

		// Assert
		cut.Find("input[type='text']").Should().NotBeNull();
	}

	[Fact]
	public void Renders_Default_Placeholder()
	{
		// Arrange & Act
		var cut = Render<SearchInput>();

		// Assert
		cut.Find("input").GetAttribute("placeholder").Should().Be("Search...");
	}

	[Fact]
	public void Renders_Custom_Placeholder()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(p => p
			.Add(c => c.Placeholder, "Find issues..."));

		// Assert
		cut.Find("input").GetAttribute("placeholder").Should().Be("Find issues...");
	}

	[Fact]
	public void Renders_Default_InputId()
	{
		// Arrange & Act
		var cut = Render<SearchInput>();

		// Assert
		cut.Find("input").GetAttribute("id").Should().Be("search-input");
	}

	[Fact]
	public void Renders_Custom_InputId()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(p => p
			.Add(c => c.Id, "issue-search"));

		// Assert
		cut.Find("input").GetAttribute("id").Should().Be("issue-search");
	}

	[Fact]
	public void Renders_SearchIcon_Always()
	{
		// Arrange & Act
		var cut = Render<SearchInput>();

		// Assert — the icon SVG is pointer-events-none and aria-hidden
		cut.Find("svg[aria-hidden='true']").Should().NotBeNull();
	}

	[Fact]
	public void Renders_AriaLabel_OnInput()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(p => p
			.Add(c => c.AriaLabel, "Search issues"));

		// Assert
		cut.Find("input").GetAttribute("aria-label").Should().Be("Search issues");
	}

	#endregion

	#region Clear Button Visibility

	[Fact]
	public void ClearButton_NotRendered_WhenValueIsNull()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(p => p
			.Add(c => c.Value, (string?)null));

		// Assert
		cut.FindAll("button[aria-label='Clear search']").Should().BeEmpty();
	}

	[Fact]
	public void ClearButton_NotRendered_WhenValueIsEmpty()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(p => p
			.Add(c => c.Value, ""));

		// Assert
		cut.FindAll("button[aria-label='Clear search']").Should().BeEmpty();
	}

	[Fact]
	public void ClearButton_Rendered_WhenValueIsNotEmpty()
	{
		// Arrange & Act
		var cut = Render<SearchInput>(p => p
			.Add(c => c.Value, "blazor"));

		// Assert
		cut.Find("button[aria-label='Clear search']").Should().NotBeNull();
	}

	#endregion

	#region ClearSearch Tests (direct path, no timer)

	[Fact]
	public async Task ClearButton_Click_FiresValueChanged_WithNull()
	{
		// Arrange — "sentinel" lets us detect if callback was never called
		string? capturedValue = "sentinel";
		var cut = Render<SearchInput>(p => p
			.Add(c => c.Value, "some query")
			.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => capturedValue = v)));

		// Act
		var clearButton = cut.Find("button[aria-label='Clear search']");
		await cut.InvokeAsync(() => clearButton.Click());

		// Assert — ClearSearch invokes ValueChanged(null) without going through the debounce timer
		capturedValue.Should().BeNull();
	}

	[Fact]
	public async Task ClearButton_Click_FiresOnSearch_WithNull()
	{
		// Arrange
		string? capturedSearch = "sentinel";
		var cut = Render<SearchInput>(p => p
			.Add(c => c.Value, "some query")
			.Add(c => c.OnSearch, EventCallback.Factory.Create<string?>(this, v => capturedSearch = v)));

		// Act
		var clearButton = cut.Find("button[aria-label='Clear search']");
		await cut.InvokeAsync(() => clearButton.Click());

		// Assert
		capturedSearch.Should().BeNull();
	}

	[Fact]
	public async Task ClearButton_Click_HidesClearButton()
	{
		// Arrange
		var cut = Render<SearchInput>(p => p
			.Add(c => c.Value, "some query")
			.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => { })));

		// Act
		var clearButton = cut.Find("button[aria-label='Clear search']");
		await cut.InvokeAsync(() => clearButton.Click());

		// Assert — after clearing, Value is null so clear button should vanish
		cut.FindAll("button[aria-label='Clear search']").Should().BeEmpty();
	}

	#endregion

	#region Debounce Input Tests

	[Fact]
	public async Task Input_Change_FiresValueChanged_AfterDebounce()
	{
		// Arrange — TaskCompletionSource avoids wall-clock racing; the callback
		// completes the task and we await with a generous timeout for CI headroom.
		var tcs = new TaskCompletionSource<string?>();
		var cut = Render<SearchInput>(p => p
			.Add(c => c.DebounceMs, 100)
			.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => tcs.TrySetResult(v))));

		// Act
		var input = cut.Find("input");
		await cut.InvokeAsync(() => input.Input("blazor"));

		// Assert — wait for debounce to fire (100 ms) with a 2-second CI safety margin
		var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
		result.Should().Be("blazor");
	}

	[Fact]
	public async Task Input_Change_FiresOnSearch_AfterDebounce()
	{
		// Arrange
		var tcs = new TaskCompletionSource<string?>();
		var cut = Render<SearchInput>(p => p
			.Add(c => c.DebounceMs, 100)
			.Add(c => c.OnSearch, EventCallback.Factory.Create<string?>(this, v => tcs.TrySetResult(v))));

		// Act
		var input = cut.Find("input");
		await cut.InvokeAsync(() => input.Input("issues"));

		// Assert
		var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
		result.Should().Be("issues");
	}

	#endregion
}
