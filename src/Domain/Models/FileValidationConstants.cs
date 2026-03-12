// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     FileValidationConstants.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

/// <summary>
///   Constants for file validation.
/// </summary>
public static class FileValidationConstants
{
	/// <summary>
	///   Maximum file size in bytes (10MB).
	/// </summary>
	public const long MAX_FILE_SIZE = 10 * 1024 * 1024;

	/// <summary>
	///   Maximum thumbnail width in pixels.
	/// </summary>
	public const int THUMBNAIL_WIDTH = 200;

	/// <summary>
	///   Maximum thumbnail height in pixels.
	/// </summary>
	public const int THUMBNAIL_HEIGHT = 200;

	/// <summary>
	///   Allowed image content types.
	/// </summary>
	public static readonly string[] ALLOWED_IMAGE_TYPES =
	[
		"image/jpeg",
		"image/png",
		"image/gif",
		"image/webp"
	];

	/// <summary>
	///   Allowed document content types.
	/// </summary>
	public static readonly string[] ALLOWED_DOCUMENT_TYPES =
	[
		"application/pdf",
		"text/plain",
		"text/markdown"
	];

	/// <summary>
	///   All allowed content types.
	/// </summary>
	public static readonly string[] ALLOWED_CONTENT_TYPES =
		[.. ALLOWED_IMAGE_TYPES, .. ALLOWED_DOCUMENT_TYPES];

	/// <summary>
	///   Checks if a content type is an image.
	/// </summary>
	public static bool IsImage(string contentType) =>
		ALLOWED_IMAGE_TYPES.Contains(contentType, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///   Checks if a content type is allowed.
	/// </summary>
	public static bool IsAllowedContentType(string contentType) =>
		ALLOWED_CONTENT_TYPES.Contains(contentType, StringComparer.OrdinalIgnoreCase);
}
