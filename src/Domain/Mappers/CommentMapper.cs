// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CommentMapper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Mappers;

/// <summary>
///   Static mapper for Comment and CommentDto conversions.
/// </summary>
public static class CommentMapper
{
	/// <summary>
	///   Converts a Comment model to a CommentDto.
	/// </summary>
	/// <param name="comment">The comment model.</param>
	/// <returns>A CommentDto instance.</returns>
	public static CommentDto ToDto(Comment? comment)
	{
		if (comment is null) { return CommentDto.Empty; }

		return new CommentDto(
			comment.Id,
			comment.Title,
			comment.Description,
			comment.DateCreated,
			comment.DateModified,
			comment.Issue,
			comment.Author,
			comment.UserVotes,
			comment.Archived,
			comment.ArchivedBy,
			comment.IsAnswer,
			comment.AnswerSelectedBy);
	}

	/// <summary>
	///   Converts a CommentDto to a Comment model.
	/// </summary>
	/// <param name="dto">The comment DTO.</param>
	/// <returns>A Comment model instance.</returns>
	public static Comment ToModel(CommentDto? dto)
	{
		if (dto is null) { return new Comment(); }

		return new Comment
		{
			Id = dto.Id,
			Title = dto.Title,
			Description = dto.Description,
			DateCreated = dto.DateCreated,
			DateModified = dto.DateModified,
			Issue = dto.Issue,
			Author = dto.Author,
			UserVotes = dto.UserVotes,
			Archived = dto.Archived,
			ArchivedBy = dto.ArchivedBy,
			IsAnswer = dto.IsAnswer,
			AnswerSelectedBy = dto.AnswerSelectedBy
		};
	}

	/// <summary>
	///   Converts a collection of Comment models to a list of CommentDto instances.
	/// </summary>
	/// <param name="comments">The comment models.</param>
	/// <returns>A list of CommentDto instances.</returns>
	public static List<CommentDto> ToDtoList(IEnumerable<Comment>? comments)
	{
		if (comments is null) { return []; }

		return comments.Select(c => ToDto(c)).ToList();
	}
}
