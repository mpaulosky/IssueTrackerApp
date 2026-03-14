// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     StatusMapper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Domain
// =============================================

namespace Domain.Mappers;

/// <summary>
///   Static mapper for Status, StatusDto, and StatusInfo conversions.
/// </summary>
public static class StatusMapper
{
	/// <summary>
	///   Converts a Status model to a StatusDto.
	/// </summary>
	/// <param name="status">The status model.</param>
	/// <returns>A StatusDto instance.</returns>
	public static StatusDto ToDto(Status? status)
	{
		if (status is null) { return StatusDto.Empty; }

		return new StatusDto(
			status.Id,
			status.StatusName,
			status.StatusDescription,
			status.DateCreated,
			status.DateModified,
			status.Archived,
			UserMapper.ToDto(status.ArchivedBy));
	}

	/// <summary>
	///   Converts a StatusInfo value object to a StatusDto.
	/// </summary>
	/// <param name="info">The status info value object.</param>
	/// <returns>A StatusDto instance.</returns>
	public static StatusDto ToDto(StatusInfo? info)
	{
		if (info is null) { return StatusDto.Empty; }

		return new StatusDto(
			info.Id,
			info.StatusName,
			info.StatusDescription,
			info.DateCreated,
			info.DateModified,
			info.Archived,
			UserMapper.ToDto(info.ArchivedBy));
	}

	/// <summary>
	///   Converts a StatusDto to a Status model.
	/// </summary>
	/// <param name="dto">The status DTO.</param>
	/// <returns>A Status model instance.</returns>
	public static Status ToModel(StatusDto? dto)
	{
		if (dto is null) { return new Status(); }

		return new Status
		{
			Id = dto.Id,
			StatusName = dto.StatusName,
			StatusDescription = dto.StatusDescription,
			DateCreated = dto.DateCreated,
			DateModified = dto.DateModified,
			Archived = dto.Archived,
			ArchivedBy = UserMapper.ToInfo(dto.ArchivedBy)
		};
	}

	/// <summary>
	///   Converts a StatusDto to a StatusInfo value object.
	/// </summary>
	/// <param name="dto">The status DTO.</param>
	/// <returns>A StatusInfo instance.</returns>
	public static StatusInfo ToInfo(StatusDto? dto)
	{
		if (dto is null) { return StatusInfo.Empty; }

		return new StatusInfo
		{
			Id = dto.Id,
			StatusName = dto.StatusName,
			StatusDescription = dto.StatusDescription,
			DateCreated = dto.DateCreated,
			DateModified = dto.DateModified,
			Archived = dto.Archived,
			ArchivedBy = UserMapper.ToInfo(dto.ArchivedBy)
		};
	}

	/// <summary>
	///   Converts a collection of Status models to a list of StatusDto instances.
	/// </summary>
	/// <param name="statuses">The status models.</param>
	/// <returns>A list of StatusDto instances.</returns>
	public static List<StatusDto> ToDtoList(IEnumerable<Status>? statuses)
	{
		if (statuses is null) { return []; }

		return statuses.Select(s => ToDto(s)).ToList();
	}
}
