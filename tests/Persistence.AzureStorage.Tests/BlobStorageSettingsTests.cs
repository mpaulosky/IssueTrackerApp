// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageSettingsTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests
// =======================================================

namespace Persistence.AzureStorage.Tests;

/// <summary>
///   Unit tests for BlobStorageSettings configuration class.
/// </summary>
public sealed class BlobStorageSettingsTests
{
	[Fact]
	public void SectionName_ShouldBeBlobStorage()
	{
		// Arrange & Act
		var sectionName = BlobStorageSettings.SECTION_NAME;

		// Assert
		sectionName.Should().Be("BlobStorage");
	}

	[Fact]
	public void ConnectionString_ShouldDefaultToEmptyString()
	{
		// Arrange & Act
		var settings = new BlobStorageSettings();

		// Assert
		settings.ConnectionString.Should().BeEmpty();
	}

	[Fact]
	public void ContainerName_ShouldDefaultToIssueAttachments()
	{
		// Arrange & Act
		var settings = new BlobStorageSettings();

		// Assert
		settings.ContainerName.Should().Be("issue-attachments");
	}

	[Fact]
	public void ThumbnailContainerName_ShouldDefaultToIssueAttachmentsThumbnails()
	{
		// Arrange & Act
		var settings = new BlobStorageSettings();

		// Assert
		settings.ThumbnailContainerName.Should().Be("issue-attachments-thumbnails");
	}

	[Fact]
	public void ConnectionString_CanBeSetToCustomValue()
	{
		// Arrange
		var settings = new BlobStorageSettings();
		var customConnectionString = "DefaultEndpointsProtocol=https;AccountName=custom;AccountKey=Y3VzdG9t";

		// Act
		settings.ConnectionString = customConnectionString;

		// Assert
		settings.ConnectionString.Should().Be(customConnectionString);
	}

	[Fact]
	public void ContainerName_CanBeSetToCustomValue()
	{
		// Arrange
		var settings = new BlobStorageSettings();
		var customContainerName = "my-custom-container";

		// Act
		settings.ContainerName = customContainerName;

		// Assert
		settings.ContainerName.Should().Be(customContainerName);
	}

	[Fact]
	public void ThumbnailContainerName_CanBeSetToCustomValue()
	{
		// Arrange
		var settings = new BlobStorageSettings();
		var customThumbnailName = "my-custom-thumbnails";

		// Act
		settings.ThumbnailContainerName = customThumbnailName;

		// Assert
		settings.ThumbnailContainerName.Should().Be(customThumbnailName);
	}
}
