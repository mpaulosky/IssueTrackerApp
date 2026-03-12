// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Attachment.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

/// <summary>
///   Attachment class - represents a file attachment on an issue.
/// </summary>
[Serializable]
public class Attachment
{
	/// <summary>
	///   Gets or sets the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the issue identifier this attachment belongs to.
	/// </summary>
	/// <value>
	///   The issue identifier.
	/// </value>
	[BsonElement("issue_id")]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId IssueId { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the name of the file.
	/// </summary>
	/// <value>
	///   The name of the file.
	/// </value>
	[BsonElement("file_name")]
	[BsonRepresentation(BsonType.String)]
	public string FileName { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the type of the content.
	/// </summary>
	/// <value>
	///   The type of the content.
	/// </value>
	[BsonElement("content_type")]
	[BsonRepresentation(BsonType.String)]
	public string ContentType { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the size of the file in bytes.
	/// </summary>
	/// <value>
	///   The size of the file.
	/// </value>
	[BsonElement("file_size")]
	[BsonRepresentation(BsonType.Int64)]
	public long FileSize { get; set; }

	/// <summary>
	///   Gets or sets the BLOB URL where the file is stored.
	/// </summary>
	/// <value>
	///   The BLOB URL.
	/// </value>
	[BsonElement("blob_url")]
	[BsonRepresentation(BsonType.String)]
	public string BlobUrl { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the thumbnail URL (for images only).
	/// </summary>
	/// <value>
	///   The thumbnail URL.
	/// </value>
	[BsonElement("thumbnail_url")]
	[BsonRepresentation(BsonType.String)]
	public string? ThumbnailUrl { get; set; }

	/// <summary>
	///   Gets or sets the user who uploaded this attachment.
	/// </summary>
	/// <value>
	///   The uploaded by user.
	/// </value>
	[BsonElement("uploaded_by")]
	public UserDto UploadedBy { get; set; } = UserDto.Empty;

	/// <summary>
	///   Gets or sets the uploaded date.
	/// </summary>
	/// <value>
	///   The uploaded date.
	/// </value>
	[BsonElement("uploaded_at")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
