// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AttachmentDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.DTOs;

/// <summary>
///   Data transfer object for attachment.
/// </summary>
[method: JsonConstructor]
public record AttachmentDto(
	string Id,
	string IssueId,
	string FileName,
	string ContentType,
	long FileSize,
	string BlobUrl,
	string? ThumbnailUrl,
	UserDto UploadedBy,
	DateTime UploadedAt)
{
	/// <summary>
	///   Creates an AttachmentDto from an Attachment model.
	/// </summary>
	public AttachmentDto(Attachment attachment) : this(
		attachment.Id.ToString(),
		attachment.IssueId.ToString(),
		attachment.FileName,
		attachment.ContentType,
		attachment.FileSize,
		attachment.BlobUrl,
		attachment.ThumbnailUrl,
		UserMapper.ToDto(attachment.UploadedBy),
		attachment.UploadedAt)
	{
	}

	/// <summary>
	///   Gets the file size formatted for display.
	/// </summary>
	public string FileSizeFormatted => FormatFileSize(FileSize);

	/// <summary>
	///   Gets a value indicating whether this attachment is an image.
	/// </summary>
	public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

	private static string FormatFileSize(long bytes)
	{
		string[] sizes = ["B", "KB", "MB", "GB"];
		double len = bytes;
		int order = 0;
		while (len >= 1024 && order < sizes.Length - 1)
		{
			order++;
			len = len / 1024;
		}
		return $"{len:0.##} {sizes[order]}";
	}

	/// <summary>
	///   Gets an empty AttachmentDto.
	/// </summary>
	public static AttachmentDto Empty => new(
		string.Empty,
		string.Empty,
		string.Empty,
		string.Empty,
		0,
		string.Empty,
		null,
		UserDto.Empty,
		DateTime.MinValue);
}
