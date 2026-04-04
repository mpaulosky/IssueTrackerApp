// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DeleteConfirmationModalTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Shared;

namespace Web.Tests.Bunit.Components.Shared;

/// <summary>
///   Tests for the DeleteConfirmationModal component.
/// </summary>
public sealed class DeleteConfirmationModalTests : BunitTestBase
{
	#region Visibility Tests

	[Fact]
	public void Modal_IsNotVisible_WhenIsVisibleIsFalse()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, false));

		// Assert — the @if (IsVisible) guard means nothing renders
		cut.Markup.Trim().Should().BeEmpty();
	}

	[Fact]
	public void Modal_IsVisible_WhenIsVisibleIsTrue()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true));

		// Assert — modal-overlay div should be in the DOM
		cut.Find("div[role='dialog']").Should().NotBeNull();
	}

	#endregion

	#region Content Rendering Tests

	[Fact]
	public void Modal_RendersDefaultTitle()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true));

		// Assert — default Title is "Delete Confirmation"
		cut.Find("h3#modal-title").TextContent.Trim().Should().Be("Delete Confirmation");
	}

	[Fact]
	public void Modal_RendersCustomTitle()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Title, "Remove Issue"));

		// Assert
		cut.Find("h3#modal-title").TextContent.Trim().Should().Be("Remove Issue");
	}

	[Fact]
	public void Modal_RendersDefaultMessage()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true));

		// Assert — default Message
		cut.Markup.Should().Contain("Are you sure you want to delete this item?");
	}

	[Fact]
	public void Modal_RendersCustomMessage()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.Message, "This will permanently remove the issue."));

		// Assert
		cut.Markup.Should().Contain("This will permanently remove the issue.");
	}

	[Fact]
	public void Modal_RendersItemTitle_WhenProvided()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.ItemTitle, "Bug: NPE in login flow"));

		// Assert — item title is wrapped in quotes in the template
		cut.Markup.Should().Contain("Bug: NPE in login flow");
	}

	[Fact]
	public void Modal_DoesNotRenderItemTitle_WhenNull()
	{
		// Arrange & Act — ItemTitle defaults to null
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.ItemTitle, (string?)null));

		// Assert — only one <p> (the message), not a second one for the item title
		var paragraphs = cut.FindAll("p");
		paragraphs.Should().HaveCount(1);
	}

	[Fact]
	public void Modal_DoesNotRenderItemTitle_WhenEmpty()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.ItemTitle, ""));

		// Assert
		var paragraphs = cut.FindAll("p");
		paragraphs.Should().HaveCount(1);
	}

	[Fact]
	public void Modal_RendersCustomConfirmButtonText()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.ConfirmButtonText, "Yes, remove it"));

		// Assert
		cut.Find("button.btn-danger span").TextContent.Should().Contain("Yes, remove it");
	}

	[Fact]
	public void Modal_RendersCustomCancelButtonText()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.CancelButtonText, "Keep it"));

		// Assert
		cut.Find("button.btn-secondary").TextContent.Trim().Should().Be("Keep it");
	}

	#endregion

	#region EventCallback Tests

	[Fact]
	public async Task ConfirmButton_Click_FiresOnConfirm()
	{
		// Arrange
		var wasCalled = false;
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.OnConfirm, EventCallback.Factory.Create(this, () => wasCalled = true)));

		// Act — confirm button has class btn-danger
		var confirmButton = cut.Find("button.btn-danger");
		await cut.InvokeAsync(() => confirmButton.Click());

		// Assert
		wasCalled.Should().BeTrue();
	}

	[Fact]
	public async Task CancelButton_Click_FiresOnCancel()
	{
		// Arrange
		var wasCalled = false;
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => wasCalled = true)));

		// Act — cancel button has class btn-secondary
		var cancelButton = cut.Find("button.btn-secondary");
		await cut.InvokeAsync(() => cancelButton.Click());

		// Assert
		wasCalled.Should().BeTrue();
	}

	[Fact]
	public async Task CancelButton_Click_FiresIsVisibleChanged_WithFalse()
	{
		// Arrange — OnCancelClick calls IsVisibleChanged.InvokeAsync(false) before OnCancel
		bool? capturedVisibility = null;
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, v => capturedVisibility = v)));

		// Act
		var cancelButton = cut.Find("button.btn-secondary");
		await cut.InvokeAsync(() => cancelButton.Click());

		// Assert
		capturedVisibility.Should().BeFalse();
	}

	[Fact]
	public async Task Backdrop_Click_FiresOnCancel()
	{
		// Arrange — the modal-backdrop div also fires OnCancelClick
		var wasCalled = false;
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => wasCalled = true)));

		// Act
		var backdrop = cut.Find("div.modal-backdrop");
		await cut.InvokeAsync(() => backdrop.Click());

		// Assert
		wasCalled.Should().BeTrue();
	}

	#endregion

	#region IsDeleting State Tests

	[Fact]
	public void ConfirmButton_IsDisabled_WhenIsDeleting()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsDeleting, true));

		// Assert
		cut.Find("button.btn-danger").GetAttribute("disabled").Should().NotBeNull();
	}

	[Fact]
	public void CancelButton_IsDisabled_WhenIsDeleting()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsDeleting, true));

		// Assert
		cut.Find("button.btn-secondary").GetAttribute("disabled").Should().NotBeNull();
	}

	[Fact]
	public void ConfirmButton_ShowsDeletingSpinner_WhenIsDeleting()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsDeleting, true));

		// Assert — animated spinner SVG and "Deleting..." span appear
		cut.Find("svg.animate-spin").Should().NotBeNull();
		cut.Find("button.btn-danger").TextContent.Should().Contain("Deleting...");
	}

	[Fact]
	public void ConfirmButton_ShowsConfirmText_WhenNotDeleting()
	{
		// Arrange & Act
		var cut = Render<DeleteConfirmationModal>(p => p
			.Add(c => c.IsVisible, true)
			.Add(c => c.IsDeleting, false)
			.Add(c => c.ConfirmButtonText, "Delete"));

		// Assert — spinner should not be present, button shows confirm text
		cut.FindAll("svg.animate-spin").Should().BeEmpty();
		cut.Find("button.btn-danger span").TextContent.Should().Be("Delete");
	}

	#endregion
}
