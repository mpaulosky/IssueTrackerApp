// =======================================================
// Copyright (c) 2025. All rights reserved.
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Services;

namespace Web.Tests.Bunit.Components.Issues;

/// <summary>
///   Tests for BulkProgressIndicator component.
/// </summary>
public class BulkProgressIndicatorTests : BunitTestBase
{
	#region Helper Methods

	/// <summary>
	///   Creates a BulkOperationProgress for use in tests.
	/// </summary>
	private static BulkOperationProgress CreateProgress(
		int total = 10,
		int processed = 0,
		int success = 0,
		int failure = 0)
	{
		return new BulkOperationProgress
		{
			TotalCount = total,
			ProcessedCount = processed,
			SuccessCount = success,
			FailureCount = failure
		};
	}

	#endregion

	#region Visibility Tests

	[Fact]
	public void BulkProgressIndicator_IsVisibleFalse_RendersNothing()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, false)
			.Add(c => c.Title, "Deleting Issues")
		);

		// Assert
		cut.Markup.Should().BeEmpty();
	}

	[Fact]
	public void BulkProgressIndicator_IsVisibleTrue_RendersModal()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Title, "Deleting Issues")
		);

		// Assert
		cut.Markup.Should().NotBeEmpty();
		cut.Find("[role='dialog']").Should().NotBeNull();
	}

	#endregion

	#region Title and Progress Tests

	[Fact]
	public void BulkProgressIndicator_IsVisibleTrue_ShowsTitle()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Title, "Updating Status")
		);

		// Assert
		cut.Markup.Should().Contain("Updating Status");
	}

	[Fact]
	public void BulkProgressIndicator_WithProgress_ShowsPercentage()
	{
		// Arrange — 5 of 10 processed = 50%
		var progress = CreateProgress(total: 10, processed: 5, success: 5);

		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Progress, progress)
		);

		// Assert — component renders "50%" via ToString("F0")
		cut.Markup.Should().Contain("50%");
	}

	[Fact]
	public void BulkProgressIndicator_WithProgress_ShowsProcessedAndTotalCounts()
	{
		// Arrange
		var progress = CreateProgress(total: 20, processed: 8, success: 8);

		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Progress, progress)
		);

		// Assert
		cut.Markup.Should().Contain("8 of 20 processed");
	}

	[Fact]
	public void BulkProgressIndicator_WithNullProgress_ShowsZeroPercent()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Progress, null)
		);

		// Assert — null Progress coalesces to 0
		cut.Markup.Should().Contain("0%");
		cut.Markup.Should().Contain("0 of 0 processed");
	}

	#endregion

	#region Spinner / Completion Tests

	[Fact]
	public void BulkProgressIndicator_NotComplete_ShowsSpinner()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsComplete, false)
		);

		// Assert — spinner SVG has class "animate-spin"
		cut.Markup.Should().Contain("animate-spin");
	}

	[Fact]
	public void BulkProgressIndicator_IsComplete_HidesSpinner()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsComplete, true)
		);

		// Assert
		cut.Markup.Should().NotContain("animate-spin");
	}

	[Fact]
	public void BulkProgressIndicator_IsCompleteWithNoFailures_ShowsSuccessMessage()
	{
		// Arrange
		var progress = CreateProgress(total: 5, processed: 5, success: 5, failure: 0);

		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsComplete, true)
			.Add(c => c.Progress, progress)
		);

		// Assert
		cut.Markup.Should().Contain("Completed successfully");
	}

	[Fact]
	public void BulkProgressIndicator_IsCompleteWithFailures_ShowsPartialSuccessMessage()
	{
		// Arrange
		var progress = CreateProgress(total: 5, processed: 5, success: 3, failure: 2);

		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsComplete, true)
			.Add(c => c.Progress, progress)
		);

		// Assert
		cut.Markup.Should().Contain("Completed with some failures");
	}

	#endregion

	#region Success and Failure Count Tests

	[Fact]
	public void BulkProgressIndicator_WithSuccessCount_ShowsSuccessCount()
	{
		// Arrange
		var progress = CreateProgress(total: 10, processed: 7, success: 7, failure: 0);

		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Progress, progress)
		);

		// Assert
		cut.Markup.Should().Contain("Success:");
		cut.Markup.Should().Contain("7");
	}

	[Fact]
	public void BulkProgressIndicator_WithFailureCountGreaterThanZero_ShowsFailureCount()
	{
		// Arrange
		var progress = CreateProgress(total: 10, processed: 10, success: 7, failure: 3);

		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Progress, progress)
		);

		// Assert
		cut.Markup.Should().Contain("Failed:");
		cut.Markup.Should().Contain("3");
	}

	[Fact]
	public void BulkProgressIndicator_WithZeroFailures_HidesFailureBlock()
	{
		// Arrange
		var progress = CreateProgress(total: 10, processed: 10, success: 10, failure: 0);

		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Progress, progress)
		);

		// Assert — the conditional block for failures is not rendered
		cut.Markup.Should().NotContain("Failed:");
	}

	#endregion

	#region Cancel Button Tests

	[Fact]
	public void BulkProgressIndicator_NotCompleteAndCanCancel_ShowsCancelButton()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsComplete, false)
			.Add(c => c.CanCancel, true)
		);

		// Assert
		cut.Markup.Should().Contain("Cancel Operation");
	}

	[Fact]
	public void BulkProgressIndicator_IsComplete_ShowsCloseButton()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsComplete, true)
		);

		// Assert — Close button appears when operation is done
		cut.Markup.Should().Contain("Close");
	}

	[Fact]
	public void BulkProgressIndicator_CloseButtonClicked_FiresOnClosed()
	{
		// Arrange
		var closedFired = false;

		var cut = Render<BulkProgressIndicator>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsComplete, true)
			.Add(c => c.OnClosed, EventCallback.Factory.Create(this, () => closedFired = true))
		);

		// Act
		cut.Find("button.btn-primary").Click();

		// Assert
		closedFired.Should().BeTrue();
	}

	#endregion
}
