// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AttachmentMapper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Mappers;

/// <summary>
///   Static mapper for Attachment and AttachmentDto conversions.
/// </summary>
public static class AttachmentMapper
{
	/// <summary>
	///   Converts an Attachment model to an AttachmentDto.
	/// </summary>
	/// <param name="attachment">The attachment model.</param>
	/// <returns>An AttachmentDto instance.</returns>
	public static AttachmentDto ToDto(Attachment? attachment)
	{
		if (attachment is null) { return AttachmentDto.Empty; }

		return new AttachmentDto(
			attachment.Id.ToString(),
			attachment.IssueId.ToString(),
			attachment.FileName,
			attachment.ContentType,
			attachment.FileSize,
			attachment.BlobUrl,
			attachment.ThumbnailUrl,
			attachment.UploadedBy,
			attachment.UploadedAt);
	}

	/// <summary>
	///   Converts an AttachmentDto to an Attachment model.
	/// </summary>
	/// <param name="dto">The attachment DTO.</param>
	/// <returns>An Attachment model instance.</returns>
	public static Attachment ToModel(AttachmentDto? dto)
	{
		if (dto is null) { return new Attachment(); }

		return new Attachment
		{
			Id = ObjectId.TryParse(dto.Id, out ObjectId id) ? id : ObjectId.Empty,
			IssueId = ObjectId.TryParse(dto.IssueId, out ObjectId issueId) ? issueId : ObjectId.Empty,
			FileName = dto.FileName,
			ContentType = dto.ContentType,
			FileSize = dto.FileSize,
			BlobUrl = dto.BlobUrl,
			ThumbnailUrl = dto.ThumbnailUrl,
			UploadedBy = dto.UploadedBy,
			UploadedAt = dto.UploadedAt
		};
	}

	/// <summary>
	///   Converts a collection of Attachment models to a list of AttachmentDto instances.
	/// </summary>
	/// <param name="attachments">The attachment models.</param>
	/// <returns>A list of AttachmentDto instances.</returns>
	public static List<AttachmentDto> ToDtoList(IEnumerable<Attachment>? attachments)
	{
		if (attachments is null) { return []; }

		return attachments.Select(a => ToDto(a)).ToList();
	}
}
