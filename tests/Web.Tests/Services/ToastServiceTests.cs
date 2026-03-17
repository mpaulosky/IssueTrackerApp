// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ToastServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for ToastService state management and event behavior.
/// </summary>
public sealed class ToastServiceTests
{
	private readonly ToastService _sut;

	public ToastServiceTests()
	{
		_sut = new ToastService();
	}

	#region ShowInfo Tests

	[Fact]
	public void ShowInfo_WithMessage_CreatesInfoToastWithCorrectType()
	{
		// Arrange
		var message = "Test info message";

		// Act
		_sut.ShowInfo(message);

		// Assert
		_sut.Toasts.Should().HaveCount(1);
		_sut.Toasts[0].Type.Should().Be(ToastType.Info);
		_sut.Toasts[0].Message.Should().Be(message);
	}

	[Fact]
	public void ShowInfo_WithDefaultDuration_UsesDefaultDurationOf5000Ms()
	{
		// Arrange
		var message = "Test info message";

		// Act
		_sut.ShowInfo(message);

		// Assert
		_sut.Toasts[0].DurationMs.Should().Be(5000);
	}

	[Fact]
	public void ShowInfo_WithCustomDuration_UsesSpecifiedDuration()
	{
		// Arrange
		var message = "Test info message";
		var customDuration = 10000;

		// Act
		_sut.ShowInfo(message, customDuration);

		// Assert
		_sut.Toasts[0].DurationMs.Should().Be(customDuration);
	}

	#endregion

	#region ShowSuccess Tests

	[Fact]
	public void ShowSuccess_WithMessage_CreatesSuccessToastWithCorrectType()
	{
		// Arrange
		var message = "Operation completed successfully";

		// Act
		_sut.ShowSuccess(message);

		// Assert
		_sut.Toasts.Should().HaveCount(1);
		_sut.Toasts[0].Type.Should().Be(ToastType.Success);
		_sut.Toasts[0].Message.Should().Be(message);
		_sut.Toasts[0].DurationMs.Should().Be(5000);
	}

	#endregion

	#region ShowWarning Tests

	[Fact]
	public void ShowWarning_WithMessage_CreatesWarningToastWithCorrectType()
	{
		// Arrange
		var message = "This is a warning";

		// Act
		_sut.ShowWarning(message);

		// Assert
		_sut.Toasts.Should().HaveCount(1);
		_sut.Toasts[0].Type.Should().Be(ToastType.Warning);
		_sut.Toasts[0].Message.Should().Be(message);
		_sut.Toasts[0].DurationMs.Should().Be(5000);
	}

	#endregion

	#region ShowError Tests

	[Fact]
	public void ShowError_WithMessage_CreatesErrorToastWithCorrectType()
	{
		// Arrange
		var message = "An error occurred";

		// Act
		_sut.ShowError(message);

		// Assert
		_sut.Toasts.Should().HaveCount(1);
		_sut.Toasts[0].Type.Should().Be(ToastType.Error);
		_sut.Toasts[0].Message.Should().Be(message);
		_sut.Toasts[0].DurationMs.Should().Be(5000);
	}

	#endregion

	#region RemoveToast Tests

	[Fact]
	public void RemoveToast_WithExistingId_RemovesToastFromList()
	{
		// Arrange
		_sut.ShowInfo("Toast to remove");
		var toastId = _sut.Toasts[0].Id;

		// Act
		_sut.RemoveToast(toastId);

		// Assert
		_sut.Toasts.Should().BeEmpty();
	}

	[Fact]
	public void RemoveToast_WithNonExistentId_DoesNotThrowAndLeavesListUnchanged()
	{
		// Arrange
		_sut.ShowInfo("Existing toast");
		var nonExistentId = Guid.NewGuid();

		// Act
		var act = () => _sut.RemoveToast(nonExistentId);

		// Assert
		act.Should().NotThrow();
		_sut.Toasts.Should().HaveCount(1);
	}

	#endregion

	#region OnChange Event Tests

	[Fact]
	public void ShowInfo_WhenCalled_FiresOnChangeEvent()
	{
		// Arrange
		var eventFired = false;
		_sut.OnChange += () => eventFired = true;

		// Act
		_sut.ShowInfo("Test message");

		// Assert
		eventFired.Should().BeTrue();
	}

	[Fact]
	public void RemoveToast_WhenToastExists_FiresOnChangeEvent()
	{
		// Arrange
		_sut.ShowInfo("Toast to remove");
		var toastId = _sut.Toasts[0].Id;
		var eventFired = false;
		_sut.OnChange += () => eventFired = true;

		// Act
		_sut.RemoveToast(toastId);

		// Assert
		eventFired.Should().BeTrue();
	}

	[Fact]
	public void RemoveToast_WhenToastDoesNotExist_DoesNotFireOnChangeEvent()
	{
		// Arrange
		_sut.ShowInfo("Existing toast");
		var eventFired = false;
		_sut.OnChange += () => eventFired = true;

		// Act
		_sut.RemoveToast(Guid.NewGuid());

		// Assert
		eventFired.Should().BeFalse();
	}

	#endregion

	#region Toasts Property Tests

	[Fact]
	public void Toasts_WhenMultipleToastsAdded_AccumulatesAllToasts()
	{
		// Arrange & Act
		_sut.ShowInfo("Info message");
		_sut.ShowSuccess("Success message");
		_sut.ShowWarning("Warning message");
		_sut.ShowError("Error message");

		// Assert
		_sut.Toasts.Should().HaveCount(4);
		_sut.Toasts.Select(t => t.Type).Should().ContainInOrder(
			ToastType.Info,
			ToastType.Success,
			ToastType.Warning,
			ToastType.Error);
	}

	[Fact]
	public void Toasts_ReturnsReadOnlyList_CannotBeModifiedExternally()
	{
		// Arrange
		_sut.ShowInfo("Test message");

		// Act & Assert
		_sut.Toasts.Should().BeAssignableTo<IReadOnlyList<ToastMessage>>();
	}

	#endregion
}
