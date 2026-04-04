// =======================================================
// Copyright (c) 2025. All rights reserved.
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Components.Issues;

/// <summary>
///   Tests for BulkConfirmationModal component.
/// </summary>
public class BulkConfirmationModalTests : BunitTestBase
{
	#region Visibility Tests

	[Fact]
	public void BulkConfirmationModal_IsVisibleFalse_RendersNothing()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, false)
			.Add(c => c.Title, "Delete Issues")
			.Add(c => c.Message, "This will delete all selected issues.")
			.Add(c => c.AffectedCount, 5)
		);

		// Assert
		cut.Markup.Should().BeEmpty();
	}

	[Fact]
	public void BulkConfirmationModal_IsVisibleTrue_RendersModal()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Title, "Delete Issues")
			.Add(c => c.Message, "This will delete all selected issues.")
			.Add(c => c.AffectedCount, 5)
		);

		// Assert
		cut.Markup.Should().NotBeEmpty();
		cut.Find("[role='dialog']").Should().NotBeNull();
	}

	#endregion

	#region Content Rendering Tests

	[Fact]
	public void BulkConfirmationModal_IsVisibleTrue_ShowsTitleAndMessage()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Title, "Archive Selected")
			.Add(c => c.Message, "These issues will be archived.")
			.Add(c => c.AffectedCount, 3)
		);

		// Assert
		cut.Markup.Should().Contain("Archive Selected");
		cut.Markup.Should().Contain("These issues will be archived.");
	}

	[Fact]
	public void BulkConfirmationModal_IsVisibleTrue_ShowsAffectedCountInBadge()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.AffectedCount, 12)
		);

		// Assert — badge contains the count and the summary sentence
		cut.Markup.Should().Contain("12");
		cut.Markup.Should().Contain("will be affected");
	}

	[Fact]
	public void BulkConfirmationModal_SingleAffectedItem_ShowsSingularForm()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.AffectedCount, 1)
		);

		// Assert — Blazor renders "issue will be affected" (singular, no trailing 's')
		cut.Markup.Should().Contain("1");
		// The component outputs "issue" or "issues" depending on count:
		// AffectedCount != 1 ? "s" : "" → count 1 emits no 's'
		cut.Markup.Should().Contain("issue will be affected");
		cut.Markup.Should().NotContain("issues will be affected");
	}

	[Fact]
	public void BulkConfirmationModal_PluralAffectedItems_ShowsPluralForm()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.AffectedCount, 4)
		);

		// Assert
		cut.Markup.Should().Contain("issues will be affected");
	}

	#endregion

	#region Action Type Icon Tests

	[Fact]
	public void BulkConfirmationModal_DeleteActionType_ShowsTrashIcon()
	{
		// The delete action gives the icon div a red background class
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.ActionType, BulkConfirmationModal.BulkActionType.Delete)
		);

		// Assert — red background classes applied for delete
		cut.Markup.Should().Contain("bg-red-100");
	}

	[Fact]
	public void BulkConfirmationModal_NonDeleteActionType_ShowsWarningIcon()
	{
		// StatusChange is the default non-delete type
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.ActionType, BulkConfirmationModal.BulkActionType.StatusChange)
		);

		// Assert — yellow background classes applied for non-delete actions
		cut.Markup.Should().Contain("bg-yellow-100");
	}

	#endregion

	#region Callback Tests

	[Fact]
	public void BulkConfirmationModal_ConfirmButtonClicked_FiresOnConfirm()
	{
		// Arrange
		var confirmFired = false;

		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.ConfirmButtonText, "Delete All")
			.Add(c => c.AffectedCount, 3)
			.Add(c => c.OnConfirm, EventCallback.Factory.Create(this, () => confirmFired = true))
		);

		// Act — click the confirm button (disabled=false because IsProcessing=false)
		var confirmBtn = cut.Find("button:not([class*='btn-secondary'])");
		confirmBtn.Click();

		// Assert
		confirmFired.Should().BeTrue();
	}

	[Fact]
	public void BulkConfirmationModal_CancelButtonClicked_FiresOnCancel()
	{
		// Arrange
		var cancelFired = false;

		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.AffectedCount, 2)
			.Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => cancelFired = true))
		);

		// Act — click the Cancel button
		cut.Find("button.btn-secondary").Click();

		// Assert
		cancelFired.Should().BeTrue();
	}

	[Fact]
	public void BulkConfirmationModal_CancelButtonClicked_FiresIsVisibleChangedFalse()
	{
		// Arrange
		bool? visibilityValue = null;

		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.AffectedCount, 2)
			.Add(c => c.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, v => visibilityValue = v))
		);

		// Act
		cut.Find("button.btn-secondary").Click();

		// Assert
		visibilityValue.Should().BeFalse();
	}

	[Fact]
	public void BulkConfirmationModal_BackdropClicked_FiresOnCancel()
	{
		// Arrange
		var cancelFired = false;

		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.AffectedCount, 2)
			.Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => cancelFired = true))
		);

		// Act — the backdrop div has class "modal-backdrop"
		cut.Find(".modal-backdrop").Click();

		// Assert
		cancelFired.Should().BeTrue();
	}

	[Fact]
	public void BulkConfirmationModal_IsProcessingTrue_DisablesButtons()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsProcessing, true)
			.Add(c => c.AffectedCount, 1)
		);

		// Assert — both action buttons should be disabled
		var buttons = cut.FindAll("button[disabled]");
		buttons.Count.Should().Be(2);
	}

	#endregion
}
