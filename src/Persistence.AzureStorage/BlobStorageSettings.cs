// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageSettings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage
// =======================================================

namespace Persistence.AzureStorage;

/// <summary>
///   Configuration settings for Azure Blob Storage.
/// </summary>
public class BlobStorageSettings
{
	public const string SECTION_NAME = "BlobStorage";

	/// <summary>
	///   Gets or sets the Azure Blob Storage connection string.
	/// </summary>
	public string ConnectionString { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the container name for issue attachments.
	/// </summary>
	public string ContainerName { get; set; } = "issue-attachments";

	/// <summary>
	///   Gets or sets the container name for thumbnails.
	/// </summary>
	public string ThumbnailContainerName { get; set; } = "issue-attachments-thumbnails";
}
