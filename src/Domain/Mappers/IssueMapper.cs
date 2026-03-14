// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     IssueMapper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Mappers;

/// <summary>
///   Static mapper for Issue and IssueDto conversions.
///   Uses UserMapper, CategoryMapper, and StatusMapper for nested object mappings.
/// </summary>
public static class IssueMapper
{
	/// <summary>
	///   Converts an Issue model to an IssueDto.
	/// </summary>
	/// <param name="issue">The issue model.</param>
	/// <returns>An IssueDto instance.</returns>
	public static IssueDto ToDto(Issue? issue)
	{
		if (issue is null) { return IssueDto.Empty; }

		return new IssueDto(
			issue.Id,
			issue.Title,
			issue.Description,
			issue.DateCreated,
			issue.DateModified,
			UserMapper.ToDto(issue.Author),
			CategoryMapper.ToDto(issue.Category),
			StatusMapper.ToDto(issue.Status),
			issue.Archived,
			UserMapper.ToDto(issue.ArchivedBy),
			issue.ApprovedForRelease,
			issue.Rejected);
	}

	/// <summary>
	///   Converts an IssueDto to an Issue model.
	/// </summary>
	/// <param name="dto">The issue DTO.</param>
	/// <returns>An Issue model instance.</returns>
	public static Issue ToModel(IssueDto? dto)
	{
		if (dto is null) { return new Issue(); }

		return new Issue
		{
			Id = dto.Id,
			Title = dto.Title,
			Description = dto.Description,
			DateCreated = dto.DateCreated,
			DateModified = dto.DateModified,
			Author = UserMapper.ToInfo(dto.Author),
			Category = CategoryMapper.ToInfo(dto.Category),
			Status = StatusMapper.ToInfo(dto.Status),
			Archived = dto.Archived,
			ArchivedBy = UserMapper.ToInfo(dto.ArchivedBy),
			ApprovedForRelease = dto.ApprovedForRelease,
			Rejected = dto.Rejected
		};
	}

	/// <summary>
	///   Converts a collection of Issue models to a list of IssueDto instances.
	/// </summary>
	/// <param name="issues">The issue models.</param>
	/// <returns>A list of IssueDto instances.</returns>
	public static List<IssueDto> ToDtoList(IEnumerable<Issue>? issues)
	{
		if (issues is null) { return []; }

		return issues.Select(i => ToDto(i)).ToList();
	}
}
