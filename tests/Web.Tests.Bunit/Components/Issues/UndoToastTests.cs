// =======================================================
// Copyright (c) 2025. All rights reserved.
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Components.Issues;

/// <summary>
///   Tests for UndoToast component.
///   Timer countdown is intentionally NOT tested (race-condition prone).
///   Instead, Dismiss and Undo paths are exercised directly.
/// </summary>
public class UndoToastTests : BunitTestBase
{
	#region Visibility Tests

	[Fact]
	public void UndoToast_IsVisibleFalse_RendersNothing()
	{
		// Act
		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, false)
			.Add(c => c.Message, "Operation completed")
		);

		// Assert
		cut.Markup.Should().BeEmpty();
	}

	[Fact]
	public void UndoToast_IsVisibleTrue_RendersToast()
	{
		// Act
		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "Operation completed")
			.Add(c => c.CountdownSeconds, 30) // large value avoids any timer-driven side-effects
		);

		// Assert
		cut.Markup.Should().NotBeEmpty();
	}

	#endregion

	#region Message Tests

	[Fact]
	public void UndoToast_IsVisibleTrue_ShowsMessage()
	{
		// Act
		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "5 issues archived successfully")
			.Add(c => c.CountdownSeconds, 30)
		);

		// Assert
		cut.Markup.Should().Contain("5 issues archived successfully");
	}

	#endregion

	#region Undo Button Tests

	[Fact]
	public void UndoToast_WithUndoToken_ShowsUndoButton()
	{
		// Act
		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "Operation completed")
			.Add(c => c.UndoToken, "undo-token-abc")
			.Add(c => c.CountdownSeconds, 30)
		);

		// Assert — undo button contains the "Undo" text
		cut.Markup.Should().Contain("Undo");
	}

	[Fact]
	public void UndoToast_WithNullUndoToken_HidesUndoButton()
	{
		// Act
		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "Operation completed")
			.Add(c => c.UndoToken, null)
			.Add(c => c.CountdownSeconds, 30)
		);

		// Assert
		cut.Markup.Should().NotContain("Undo");
	}

	[Fact]
	public void UndoToast_WithEmptyUndoToken_HidesUndoButton()
	{
		// Act
		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "Operation completed")
			.Add(c => c.UndoToken, string.Empty)
			.Add(c => c.CountdownSeconds, 30)
		);

		// Assert
		cut.Markup.Should().NotContain("Undo");
	}

	[Fact]
	public void UndoToast_WithUndoToken_ShowsCountdownSecondsInUndoButton()
	{
		// Arrange — use a distinctive countdown value
		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "Done")
			.Add(c => c.UndoToken, "token-xyz")
			.Add(c => c.CountdownSeconds, 42)
		);

		// Assert — the remaining seconds bubble shows the initial CountdownSeconds value
		cut.Markup.Should().Contain("42");
	}

	#endregion

	#region Dismiss (Close Button) Tests

	[Fact]
	public void UndoToast_CloseButtonClicked_FiresIsVisibleChangedFalse()
	{
		// Arrange
		bool? visibilityValue = null;

		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "Done")
			.Add(c => c.CountdownSeconds, 30)
			.Add(c => c.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, v => visibilityValue = v))
		);

		// Act — close button has sr-only text "Dismiss"
		cut.Find("button[class*='text-primary-400']").Click();

		// Assert
		visibilityValue.Should().BeFalse();
	}

	[Fact]
	public void UndoToast_CloseButtonClicked_FiresOnDismissed()
	{
		// Arrange
		var dismissedFired = false;

		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "Done")
			.Add(c => c.CountdownSeconds, 30)
			.Add(c => c.OnDismissed, EventCallback.Factory.Create(this, () => dismissedFired = true))
		);

		// Act
		cut.Find("button[class*='text-primary-400']").Click();

		// Assert
		dismissedFired.Should().BeTrue();
	}

	#endregion

	#region Undo Callback Tests

	[Fact]
	public void UndoToast_UndoButtonClicked_FiresOnUndoWithToken()
	{
		// Arrange
		string? receivedToken = null;
		const string undoToken = "bulk-undo-token-12345";

		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "3 issues deleted")
			.Add(c => c.UndoToken, undoToken)
			.Add(c => c.CountdownSeconds, 30)
			.Add(c => c.OnUndo, EventCallback.Factory.Create<string>(this, t => receivedToken = t))
		);

		// Act — click the Undo button (flex-shrink-0 inline-flex ... Undo)
		cut.Find("button[class*='bg-white/10']").Click();

		// Assert
		receivedToken.Should().Be(undoToken);
	}

	[Fact]
	public void UndoToast_UndoButtonClicked_AlsoFiresOnDismissed()
	{
		// Arrange — undo internally calls Dismiss after invoking OnUndo
		var dismissedFired = false;

		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "2 issues status changed")
			.Add(c => c.UndoToken, "token-abc")
			.Add(c => c.CountdownSeconds, 30)
			.Add(c => c.OnDismissed, EventCallback.Factory.Create(this, () => dismissedFired = true))
		);

		// Act
		cut.Find("button[class*='bg-white/10']").Click();

		// Assert
		dismissedFired.Should().BeTrue();
	}

	[Fact]
	public void UndoToast_UndoButtonClicked_FiresIsVisibleChangedFalse()
	{
		// Arrange
		bool? visibilityValue = null;

		var cut = Render<UndoToast>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "Done")
			.Add(c => c.UndoToken, "token-def")
			.Add(c => c.CountdownSeconds, 30)
			.Add(c => c.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, v => visibilityValue = v))
		);

		// Act
		cut.Find("button[class*='bg-white/10']").Click();

		// Assert — undo also dismisses the toast
		visibilityValue.Should().BeFalse();
	}

	#endregion
}
