// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LabelInputTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Shared;

namespace Web.Tests.Bunit.Components.Shared;

/// <summary>
///   Tests for the LabelInput component.
/// </summary>
public sealed class LabelInputTests : BunitTestBase
{
	#region Render Tests

	[Fact]
	public void Renders_WithNoLabels_ShowsPlaceholderText()
	{
		// Arrange & Act
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, [])
		);

		// Assert
		var input = cut.Find("input#label-input");
		input.Should().NotBeNull();
		input.GetAttribute("placeholder").Should().Contain("Add labels");
	}

	[Fact]
	public void Renders_WithLabels_ShowsPillsForEachLabel()
	{
		// Arrange & Act
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, ["bug", "v2"])
		);

		// Assert — each label pill is a <span> with the label text
		var pills = cut.FindAll("span.rounded");
		pills.Should().HaveCount(2);
		cut.Markup.Should().Contain("bug");
		cut.Markup.Should().Contain("v2");
	}

	[Fact]
	public async Task RemoveButton_WhenClicked_RemovesLabel()
	{
		// Arrange
		List<string>? capturedLabels = null;
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, ["bug", "v2"])
			.Add(p => p.LabelsChanged,
				EventCallback.Factory.Create<List<string>>(this, list => capturedLabels = list))
		);

		// Act — click the remove button for "bug"
		var removeButton = cut.Find("button[aria-label='Remove label bug']");
		await cut.InvokeAsync(() => removeButton.Click());

		// Assert
		capturedLabels.Should().NotBeNull();
		capturedLabels.Should().NotContain("bug");
		capturedLabels.Should().Contain("v2");
	}

	[Fact]
	public async Task Input_WhenEnterPressed_AddsLabel()
	{
		// Arrange
		List<string>? capturedLabels = null;
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, [])
			.Add(p => p.LabelsChanged,
				EventCallback.Factory.Create<List<string>>(this, list => capturedLabels = list))
		);

		var input = cut.Find("input#label-input");

		// Act — set input value then press Enter
		await cut.InvokeAsync(() => input.Input("feature"));
		await cut.InvokeAsync(() => input.KeyDown(Key.Enter));

		// Assert
		capturedLabels.Should().NotBeNull();
		capturedLabels.Should().Contain("feature");
	}

	[Fact]
	public async Task Input_WhenCommaTyped_AddsLabel()
	{
		// Arrange
		List<string>? capturedLabels = null;
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, [])
			.Add(p => p.LabelsChanged,
				EventCallback.Factory.Create<List<string>>(this, list => capturedLabels = list))
		);

		var input = cut.Find("input#label-input");

		// Act — simulate typing "bug" then pressing the comma key
		await cut.InvokeAsync(() => input.Input("bug"));
		await cut.InvokeAsync(() => input.KeyDown(","));

		// Assert
		capturedLabels.Should().NotBeNull();
		capturedLabels.Should().Contain("bug");
	}

	[Fact]
	public void Input_AtMaxLabels_HidesInput()
	{
		// Arrange — 10 labels (== MaxLabels default)
		var labels = Enumerable.Range(1, 10).Select(i => $"label-{i}").ToList();

		// Act
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, labels)
		);

		// Assert
		cut.FindAll("input#label-input").Should().BeEmpty();
	}

	[Fact]
	public void Input_AtMaxLabels_ShowsWarning()
	{
		// Arrange — 10 labels (== MaxLabels default)
		var labels = Enumerable.Range(1, 10).Select(i => $"label-{i}").ToList();

		// Act
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, labels)
		);

		// Assert
		var warning = cut.Find("p[role='status']");
		warning.Should().NotBeNull();
		warning.TextContent.Should().Contain("Maximum of 10 labels reached");
	}

	[Fact]
	public async Task Input_WhenDuplicateLabel_DoesNotAdd()
	{
		// Arrange
		var callCount = 0;
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, ["bug"])
			.Add(p => p.LabelsChanged,
				EventCallback.Factory.Create<List<string>>(this, _ => callCount++))
		);

		var input = cut.Find("input#label-input");

		// Act — attempt to add "bug" again (case-insensitive duplicate)
		await cut.InvokeAsync(() => input.Input("bug"));
		await cut.InvokeAsync(() => input.KeyDown(Key.Enter));

		// Assert — callback should NOT have been fired (duplicate is silently ignored)
		callCount.Should().Be(0);
	}

	[Fact]
	public async Task Autocomplete_WhenTextTyped_CallsLabelService()
	{
		// Arrange — override mock to return suggestions
		LabelService.GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<IReadOnlyList<string>>(["bug", "feature", "v2"]));

		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, [])
		);

		var input = cut.Find("input#label-input");

		// Act — type enough characters to trigger a suggestion fetch
		await cut.InvokeAsync(() => input.Input("fea"));

		// Wait for the 300 ms debounce to elapse and the async fetch to complete
		await Task.Delay(500);

		// Assert
		await LabelService.Received(1)
			.GetSuggestionsAsync(Arg.Is("fea"), Arg.Any<int>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Autocomplete_WhenSuggestionClicked_AddsLabel()
	{
		// Arrange — override mock to return suggestions
		LabelService.GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<IReadOnlyList<string>>(["feature", "bug", "v2"]));

		List<string>? capturedLabels = null;
		var cut = Render<LabelInput>(parameters => parameters
			.Add(p => p.Labels, [])
			.Add(p => p.LabelsChanged,
				EventCallback.Factory.Create<List<string>>(this, list => capturedLabels = list))
		);

		var input = cut.Find("input#label-input");

		// Act — type text to trigger autocomplete
		await cut.InvokeAsync(() => input.Input("fea"));

		// Wait for debounce + StateHasChanged re-render
		await cut.WaitForStateAsync(
			() => cut.FindAll("button[role='option']").Count > 0,
			TimeSpan.FromSeconds(2));

		// Click the first suggestion
		var suggestion = cut.Find("button[role='option']");
		await cut.InvokeAsync(() => suggestion.Click());

		// Assert
		capturedLabels.Should().NotBeNull();
		capturedLabels.Should().Contain("feature");
	}

	#endregion
}
