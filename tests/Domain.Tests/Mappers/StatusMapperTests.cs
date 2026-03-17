// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusMapperTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Mappers;

/// <summary>
///   Unit tests for StatusMapper static mapping methods.
/// </summary>
public sealed class StatusMapperTests
{
	private readonly DateTime _dateCreated = new(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
	private readonly DateTime _dateModified = new(2025, 2, 20, 14, 45, 0, DateTimeKind.Utc);

	#region ToDto(Status) Tests

	[Fact]
	public void ToDto_FromStatus_WithValidStatus_ReturnsCorrectDto()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var archivedBy = new UserInfo
		{
			Id = "auth0|archiver",
			Name = "Archiver User",
			Email = "archiver@example.com"
		};
		var status = new Status
		{
			Id = statusId,
			StatusName = "Open",
			StatusDescription = "Issue is open and awaiting action",
			DateCreated = _dateCreated,
			DateModified = _dateModified,
			Archived = true,
			ArchivedBy = archivedBy
		};

		// Act
		var result = StatusMapper.ToDto(status);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(statusId);
		result.StatusName.Should().Be("Open");
		result.StatusDescription.Should().Be("Issue is open and awaiting action");
		result.DateCreated.Should().Be(_dateCreated);
		result.DateModified.Should().Be(_dateModified);
		result.Archived.Should().BeTrue();
		result.ArchivedBy.Id.Should().Be("auth0|archiver");
	}

	[Fact]
	public void ToDto_FromStatus_WithNullStatus_ReturnsEmptyDto()
	{
		// Arrange
		Status? status = null;

		// Act
		var result = StatusMapper.ToDto(status);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.StatusName.Should().BeEmpty();
		result.StatusDescription.Should().BeEmpty();
	}

	[Fact]
	public void ToDto_FromStatus_WithNonArchivedStatus_ReturnsArchivedFalse()
	{
		// Arrange
		var status = new Status
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "In Progress",
			StatusDescription = "Work is in progress",
			DateCreated = _dateCreated,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		// Act
		var result = StatusMapper.ToDto(status);

		// Assert
		result.Archived.Should().BeFalse();
		result.DateModified.Should().BeNull();
		result.ArchivedBy.Id.Should().BeEmpty();
		result.ArchivedBy.Name.Should().BeEmpty();
		result.ArchivedBy.Email.Should().BeEmpty();
	}

	[Fact]
	public void ToDto_FromStatus_MapsArchivedByCorrectly()
	{
		// Arrange
		var archivedBy = new UserInfo
		{
			Id = "auth0|admin",
			Name = "Admin User",
			Email = "admin@example.com"
		};
		var status = new Status
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Deprecated",
			StatusDescription = "Status no longer in use",
			Archived = true,
			ArchivedBy = archivedBy
		};

		// Act
		var result = StatusMapper.ToDto(status);

		// Assert
		result.ArchivedBy.Should().NotBeNull();
		result.ArchivedBy.Id.Should().Be("auth0|admin");
		result.ArchivedBy.Name.Should().Be("Admin User");
		result.ArchivedBy.Email.Should().Be("admin@example.com");
	}

	#endregion

	#region ToDto(StatusInfo) Tests

	[Fact]
	public void ToDto_FromStatusInfo_WithValidStatusInfo_ReturnsCorrectDto()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var archivedBy = new UserInfo
		{
			Id = "auth0|info-archiver",
			Name = "Info Archiver",
			Email = "info@example.com"
		};
		var statusInfo = new StatusInfo
		{
			Id = statusId,
			StatusName = "Closed",
			StatusDescription = "Issue has been closed",
			DateCreated = _dateCreated,
			DateModified = _dateModified,
			Archived = false,
			ArchivedBy = archivedBy
		};

		// Act
		var result = StatusMapper.ToDto(statusInfo);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(statusId);
		result.StatusName.Should().Be("Closed");
		result.StatusDescription.Should().Be("Issue has been closed");
		result.DateCreated.Should().Be(_dateCreated);
		result.DateModified.Should().Be(_dateModified);
		result.Archived.Should().BeFalse();
	}

	[Fact]
	public void ToDto_FromStatusInfo_WithNullStatusInfo_ReturnsEmptyDto()
	{
		// Arrange
		StatusInfo? statusInfo = null;

		// Act
		var result = StatusMapper.ToDto(statusInfo);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.StatusName.Should().BeEmpty();
	}

	[Fact]
	public void ToDto_FromStatusInfo_WithEmptyStatusInfo_ReturnsEmptyValuesDto()
	{
		// Arrange
		var statusInfo = StatusInfo.Empty;

		// Act
		var result = StatusMapper.ToDto(statusInfo);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.StatusName.Should().BeEmpty();
		result.StatusDescription.Should().BeEmpty();
	}

	#endregion

	#region ToModel Tests

	[Fact]
	public void ToModel_WithValidDto_ReturnsCorrectModel()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var archivedByDto = new UserDto("auth0|model-user", "Model User", "model@example.com");
		var dto = new StatusDto(
			statusId,
			"Resolved",
			"Issue has been resolved",
			_dateCreated,
			_dateModified,
			true,
			archivedByDto);

		// Act
		var result = StatusMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(statusId);
		result.StatusName.Should().Be("Resolved");
		result.StatusDescription.Should().Be("Issue has been resolved");
		result.DateCreated.Should().Be(_dateCreated);
		result.DateModified.Should().Be(_dateModified);
		result.Archived.Should().BeTrue();
		result.ArchivedBy.Id.Should().Be("auth0|model-user");
	}

	[Fact]
	public void ToModel_WithNullDto_ReturnsEmptyModel()
	{
		// Arrange
		StatusDto? dto = null;

		// Act
		var result = StatusMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.StatusName.Should().BeEmpty();
		result.StatusDescription.Should().BeEmpty();
	}

	[Fact]
	public void ToModel_WithEmptyDto_ReturnsDefaultModel()
	{
		// Arrange
		var dto = StatusDto.Empty;

		// Act
		var result = StatusMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.StatusName.Should().BeEmpty();
	}

	[Fact]
	public void ToModel_WithNullDateModified_SetsNullDateModified()
	{
		// Arrange
		var dto = new StatusDto(
			ObjectId.GenerateNewId(),
			"Test",
			"Test Description",
			_dateCreated,
			null,
			false,
			UserDto.Empty);

		// Act
		var result = StatusMapper.ToModel(dto);

		// Assert
		result.DateModified.Should().BeNull();
	}

	#endregion

	#region ToInfo Tests

	[Fact]
	public void ToInfo_WithValidDto_ReturnsCorrectStatusInfo()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var archivedByDto = new UserDto("auth0|info-user", "Info User", "info@example.com");
		var dto = new StatusDto(
			statusId,
			"Blocked",
			"Issue is blocked",
			_dateCreated,
			_dateModified,
			false,
			archivedByDto);

		// Act
		var result = StatusMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(statusId);
		result.StatusName.Should().Be("Blocked");
		result.StatusDescription.Should().Be("Issue is blocked");
		result.DateCreated.Should().Be(_dateCreated);
		result.DateModified.Should().Be(_dateModified);
		result.Archived.Should().BeFalse();
		result.ArchivedBy.Id.Should().Be("auth0|info-user");
	}

	[Fact]
	public void ToInfo_WithNullDto_ReturnsEmptyStatusInfo()
	{
		// Arrange
		StatusDto? dto = null;

		// Act
		var result = StatusMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.StatusName.Should().BeEmpty();
		result.StatusDescription.Should().BeEmpty();
	}

	[Fact]
	public void ToInfo_WithEmptyDto_ReturnsEmptyValuesStatusInfo()
	{
		// Arrange
		var dto = StatusDto.Empty;

		// Act
		var result = StatusMapper.ToInfo(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.StatusName.Should().BeEmpty();
		result.StatusDescription.Should().BeEmpty();
	}

	#endregion

	#region ToDtoList Tests

	[Fact]
	public void ToDtoList_WithValidStatuses_ReturnsCorrectDtoList()
	{
		// Arrange
		var statuses = new List<Status>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Open",
				StatusDescription = "Open status",
				DateCreated = _dateCreated
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "In Progress",
				StatusDescription = "Work in progress",
				DateCreated = _dateCreated
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Closed",
				StatusDescription = "Closed status",
				DateCreated = _dateCreated
			}
		};

		// Act
		var result = StatusMapper.ToDtoList(statuses);

		// Assert
		result.Should().NotBeNull();
		result.Should().HaveCount(3);
		result[0].StatusName.Should().Be("Open");
		result[1].StatusName.Should().Be("In Progress");
		result[2].StatusName.Should().Be("Closed");
	}

	[Fact]
	public void ToDtoList_WithNullCollection_ReturnsEmptyList()
	{
		// Arrange
		IEnumerable<Status>? statuses = null;

		// Act
		var result = StatusMapper.ToDtoList(statuses);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_WithEmptyCollection_ReturnsEmptyList()
	{
		// Arrange
		var statuses = new List<Status>();

		// Act
		var result = StatusMapper.ToDtoList(statuses);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_WithSingleStatus_ReturnsSingleElementList()
	{
		// Arrange
		var statuses = new List<Status>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				StatusName = "Only Status",
				StatusDescription = "The only status"
			}
		};

		// Act
		var result = StatusMapper.ToDtoList(statuses);

		// Assert
		result.Should().HaveCount(1);
		result[0].StatusName.Should().Be("Only Status");
	}

	[Fact]
	public void ToDtoList_PreservesOrderOfStatuses()
	{
		// Arrange
		var id1 = ObjectId.GenerateNewId();
		var id2 = ObjectId.GenerateNewId();
		var id3 = ObjectId.GenerateNewId();
		var statuses = new List<Status>
		{
			new() { Id = id1, StatusName = "First" },
			new() { Id = id2, StatusName = "Second" },
			new() { Id = id3, StatusName = "Third" }
		};

		// Act
		var result = StatusMapper.ToDtoList(statuses);

		// Assert
		result[0].Id.Should().Be(id1);
		result[1].Id.Should().Be(id2);
		result[2].Id.Should().Be(id3);
	}

	#endregion

	#region Round-Trip Tests

	[Fact]
	public void RoundTrip_StatusToModelToDto_PreservesData()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var archivedByDto = new UserDto("auth0|roundtrip", "RoundTrip User", "roundtrip@example.com");
		var originalDto = new StatusDto(
			statusId,
			"RoundTrip Status",
			"Testing round-trip mapping",
			_dateCreated,
			_dateModified,
			true,
			archivedByDto);

		// Act
		var model = StatusMapper.ToModel(originalDto);
		var resultDto = StatusMapper.ToDto(model);

		// Assert
		resultDto.Id.Should().Be(originalDto.Id);
		resultDto.StatusName.Should().Be(originalDto.StatusName);
		resultDto.StatusDescription.Should().Be(originalDto.StatusDescription);
		resultDto.DateCreated.Should().Be(originalDto.DateCreated);
		resultDto.DateModified.Should().Be(originalDto.DateModified);
		resultDto.Archived.Should().Be(originalDto.Archived);
		resultDto.ArchivedBy.Id.Should().Be(originalDto.ArchivedBy.Id);
	}

	[Fact]
	public void RoundTrip_StatusInfoToDtoToInfo_PreservesData()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var archivedBy = new UserInfo
		{
			Id = "auth0|infotrip",
			Name = "Info Trip User",
			Email = "infotrip@example.com"
		};
		var originalInfo = new StatusInfo
		{
			Id = statusId,
			StatusName = "Info Trip Status",
			StatusDescription = "Testing StatusInfo round-trip",
			DateCreated = _dateCreated,
			DateModified = _dateModified,
			Archived = false,
			ArchivedBy = archivedBy
		};

		// Act
		var dto = StatusMapper.ToDto(originalInfo);
		var resultInfo = StatusMapper.ToInfo(dto);

		// Assert
		resultInfo.Id.Should().Be(originalInfo.Id);
		resultInfo.StatusName.Should().Be(originalInfo.StatusName);
		resultInfo.StatusDescription.Should().Be(originalInfo.StatusDescription);
		resultInfo.DateCreated.Should().Be(originalInfo.DateCreated);
		resultInfo.DateModified.Should().Be(originalInfo.DateModified);
		resultInfo.Archived.Should().Be(originalInfo.Archived);
		resultInfo.ArchivedBy.Id.Should().Be(originalInfo.ArchivedBy.Id);
	}

	#endregion
}
