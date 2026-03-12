// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IFileStorageService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Abstractions;

/// <summary>
///   Service for managing file storage operations.
/// </summary>
public interface IFileStorageService
{
	/// <summary>
	///   Uploads a file to storage.
	/// </summary>
	/// <param name="content">The file content stream.</param>
	/// <param name="fileName">The name of the file.</param>
	/// <param name="contentType">The MIME type of the file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The URL of the uploaded file.</returns>
	Task<string> UploadAsync(
		Stream content,
		string fileName,
		string contentType,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Downloads a file from storage.
	/// </summary>
	/// <param name="blobUrl">The URL of the file to download.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The file content stream.</returns>
	Task<Stream> DownloadAsync(
		string blobUrl,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Deletes a file from storage.
	/// </summary>
	/// <param name="blobUrl">The URL of the file to delete.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task DeleteAsync(
		string blobUrl,
		CancellationToken cancellationToken = default);

	/// <summary>
	///   Generates a thumbnail for an image file.
	/// </summary>
	/// <param name="blobUrl">The URL of the image file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The URL of the generated thumbnail, or null if not applicable.</returns>
	Task<string?> GenerateThumbnailAsync(
		string blobUrl,
		CancellationToken cancellationToken = default);
}
