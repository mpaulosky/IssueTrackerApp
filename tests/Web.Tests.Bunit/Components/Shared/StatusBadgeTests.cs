// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusBadgeTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Web.Components.Shared;

namespace Web.Tests.Bunit.Components.Shared;

/// <summary>
///   Tests for the StatusBadge component.
/// </summary>
public sealed class StatusBadgeTests : BunitTestBase
{
	#region Null / Unknown Tests

	[Fact]
	public void StatusBadge_WithNullStatus_ShowsUnknown()
	{
		// Arrange & Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, (StatusDto?)null));

		// Assert
		cut.Find("span").TextContent.Trim().Should().Be("Unknown");
	}

	[Fact]
	public void StatusBadge_WithNullStatus_AppliesDefaultColorClasses()
	{
		// Arrange & Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, (StatusDto?)null));

		// Assert — default branch uses primary-100 background
		cut.Find("span").GetAttribute("class").Should().Contain("bg-primary-100");
	}

	#endregion

	#region Status Name Rendering

	[Fact]
	public void StatusBadge_WithStatus_RendersStatusName()
	{
		// Arrange
		var status = CreateTestStatus(name: "Open");

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status));

		// Assert
		cut.Find("span").TextContent.Trim().Should().Be("Open");
	}

	#endregion

	#region Color Class Tests

	[Theory]
	[InlineData("Open", "bg-primary-100")]
	[InlineData("In Progress", "bg-yellow-100")]
	[InlineData("Under Review", "bg-primary-100")]
	[InlineData("Resolved", "bg-green-100")]
	[InlineData("Closed", "bg-primary-100")]
	[InlineData("Won't Fix", "bg-red-100")]
	[InlineData("SomeUnknownStatus", "bg-primary-100")]
	public void StatusBadge_AppliesCorrectBackgroundClass(string statusName, string expectedBgClass)
	{
		// Arrange
		var status = CreateTestStatus(name: statusName);

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain(expectedBgClass);
	}

	[Fact]
	public void StatusBadge_Open_AppliesPrimaryTextColor()
	{
		// Arrange
		var status = CreateTestStatus(name: "Open");

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status));

		// Assert — "open" maps to text-primary-800
		cut.Find("span").GetAttribute("class").Should().Contain("text-primary-800");
	}

	[Fact]
	public void StatusBadge_InProgress_AppliesYellowTextColor()
	{
		// Arrange
		var status = CreateTestStatus(name: "In Progress");

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("text-yellow-800");
	}

	[Fact]
	public void StatusBadge_Resolved_AppliesGreenTextColor()
	{
		// Arrange
		var status = CreateTestStatus(name: "Resolved");

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("text-green-800");
	}

	[Fact]
	public void StatusBadge_WontFix_AppliesRedTextColor()
	{
		// Arrange
		var status = CreateTestStatus(name: "Won't Fix");

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("text-red-800");
	}

	#endregion

	#region AdditionalClasses

	[Fact]
	public void StatusBadge_WithAdditionalClasses_AppendsThemToSpan()
	{
		// Arrange
		var status = CreateTestStatus(name: "Open");

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status)
			.Add(c => c.AdditionalClasses, "my-custom-class"));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("my-custom-class");
	}

	[Fact]
	public void StatusBadge_WithNoAdditionalClasses_DoesNotAddTrailingSpace()
	{
		// Arrange
		var status = CreateTestStatus(name: "Open");

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status));

		// Assert — class should not end with a naked space
		var cssClass = cut.Find("span").GetAttribute("class") ?? "";
		cssClass.Should().NotEndWith(" ");
	}

	#endregion

	#region Base Class Presence

	[Fact]
	public void StatusBadge_AlwaysContains_BadgeBaseClass()
	{
		// Arrange
		var status = CreateTestStatus(name: "Resolved");

		// Act
		var cut = Render<StatusBadge>(p => p
			.Add(c => c.Status, status));

		// Assert — GetBadgeClasses always prepends "badge"
		cut.Find("span").GetAttribute("class").Should().StartWith("badge ");
	}

	#endregion
}

/// <summary>
///   Tests for the CategoryBadge component.
/// </summary>
public sealed class CategoryBadgeTests : BunitTestBase
{
	#region Null / Unknown Tests

	[Fact]
	public void CategoryBadge_WithNullCategory_ShowsUnknown()
	{
		// Arrange & Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, (CategoryDto?)null));

		// Assert
		cut.Find("span").TextContent.Trim().Should().Be("Unknown");
	}

	[Fact]
	public void CategoryBadge_WithNullCategory_AppliesDefaultColorClasses()
	{
		// Arrange & Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, (CategoryDto?)null));

		// Assert — default branch
		cut.Find("span").GetAttribute("class").Should().Contain("bg-primary-100");
	}

	#endregion

	#region Category Name Rendering

	[Fact]
	public void CategoryBadge_WithCategory_RendersCategoryName()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");

		// Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, category));

		// Assert
		cut.Find("span").TextContent.Trim().Should().Be("Bug");
	}

	#endregion

	#region Color Class Tests

	[Theory]
	[InlineData("Bug", "bg-red-100")]
	[InlineData("Feature", "bg-green-100")]
	[InlineData("Enhancement", "bg-primary-100")]
	[InlineData("Question", "bg-primary-100")]
	[InlineData("Documentation", "bg-yellow-100")]
	[InlineData("SomeUnknownCategory", "bg-primary-100")]
	public void CategoryBadge_AppliesCorrectBackgroundClass(string categoryName, string expectedBgClass)
	{
		// Arrange
		var category = CreateTestCategory(name: categoryName);

		// Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain(expectedBgClass);
	}

	[Fact]
	public void CategoryBadge_Bug_AppliesRedTextColor()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");

		// Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("text-red-800");
	}

	[Fact]
	public void CategoryBadge_Feature_AppliesGreenTextColor()
	{
		// Arrange
		var category = CreateTestCategory(name: "Feature");

		// Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("text-green-800");
	}

	[Fact]
	public void CategoryBadge_Documentation_AppliesYellowTextColor()
	{
		// Arrange
		var category = CreateTestCategory(name: "Documentation");

		// Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("text-yellow-800");
	}

	#endregion

	#region AdditionalClasses

	[Fact]
	public void CategoryBadge_WithAdditionalClasses_AppendsThem()
	{
		// Arrange
		var category = CreateTestCategory(name: "Feature");

		// Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, category)
			.Add(c => c.AdditionalClasses, "ml-2"));

		// Assert
		cut.Find("span").GetAttribute("class").Should().Contain("ml-2");
	}

	#endregion

	#region Base Class Presence

	[Fact]
	public void CategoryBadge_AlwaysContains_BadgeBaseClass()
	{
		// Arrange
		var category = CreateTestCategory(name: "Feature");

		// Act
		var cut = Render<CategoryBadge>(p => p
			.Add(c => c.Category, category));

		// Assert
		cut.Find("span").GetAttribute("class").Should().StartWith("badge ");
	}

	#endregion
}
