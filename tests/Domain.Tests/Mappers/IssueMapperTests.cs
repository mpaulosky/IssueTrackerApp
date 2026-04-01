// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueMapperTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Mappers;

/// <summary>
///   Unit tests for the IssueMapper class.
///   Tests Issue to IssueDto and IssueDto to Issue conversions.
/// </summary>
public sealed class IssueMapperTests
{
	#region ToDto Tests

	[Fact]
	public void ToDto_WithValidIssue_ReturnsCorrectDto()
	{
		// Arrange
		var issue = CreateTestIssue();

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(issue.Id);
		result.Title.Should().Be(issue.Title);
		result.Description.Should().Be(issue.Description);
		result.DateCreated.Should().Be(issue.DateCreated);
		result.DateModified.Should().Be(issue.DateModified);
		result.Archived.Should().Be(issue.Archived);
		result.ApprovedForRelease.Should().Be(issue.ApprovedForRelease);
		result.Rejected.Should().Be(issue.Rejected);
	}

	[Fact]
	public void ToDto_WithNullIssue_ReturnsEmptyDto()
	{
		// Arrange
		Issue? issue = null;

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.Title.Should().BeEmpty();
		result.Description.Should().BeEmpty();
	}

	[Fact]
	public void ToDto_WithAuthor_MapsAuthorToUserDto()
	{
		// Arrange
		var issue = CreateTestIssue();
		issue.Author = new UserInfo
		{
			Id = "auth0|123",
			Name = "Test Author",
			Email = "author@example.com"
		};

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Author.Should().NotBeNull();
		result.Author.Id.Should().Be("auth0|123");
		result.Author.Name.Should().Be("Test Author");
		result.Author.Email.Should().Be("author@example.com");
	}

	[Fact]
	public void ToDto_WithAssignee_MapsAssigneeToUserDto()
	{
		// Arrange
		var issue = CreateTestIssue();
		issue.Assignee = new UserInfo
		{
			Id = "auth0|assigned",
			Name = "Assigned User",
			Email = "assigned@example.com"
		};

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Assignee.Should().NotBeNull();
		result.Assignee.Id.Should().Be("auth0|assigned");
		result.Assignee.Name.Should().Be("Assigned User");
		result.Assignee.Email.Should().Be("assigned@example.com");
	}

	[Fact]
	public void ToDto_WhenAssigneeIsEmpty_MapsToEmptyUserDto()
	{
		// Arrange
		var issue = CreateTestIssue();
		issue.Assignee = UserInfo.Empty;

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Assignee.Should().BeEquivalentTo(UserDto.Empty);
	}

	[Fact]
	public void ToDto_WithCategory_MapsCategoryToCategoryDto()
	{
		// Arrange
		var issue = CreateTestIssue();
		issue.Category = new CategoryInfo
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Bug",
			CategoryDescription = "Bug category"
		};

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Category.Should().NotBeNull();
		result.Category.CategoryName.Should().Be("Bug");
		result.Category.CategoryDescription.Should().Be("Bug category");
	}

	[Fact]
	public void ToDto_WithStatus_MapsStatusToStatusDto()
	{
		// Arrange
		var issue = CreateTestIssue();
		issue.Status = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Open",
			StatusDescription = "Issue is open"
		};

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Status.Should().NotBeNull();
		result.Status.StatusName.Should().Be("Open");
		result.Status.StatusDescription.Should().Be("Issue is open");
	}

	[Fact]
	public void ToDto_WithArchivedBy_MapsArchivedByToUserDto()
	{
		// Arrange
		var issue = CreateTestIssue();
		issue.Archived = true;
		issue.ArchivedBy = new UserInfo
		{
			Id = "auth0|456",
			Name = "Admin User",
			Email = "admin@example.com"
		};

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Archived.Should().BeTrue();
		result.ArchivedBy.Should().NotBeNull();
		result.ArchivedBy.Id.Should().Be("auth0|456");
		result.ArchivedBy.Name.Should().Be("Admin User");
	}

	[Fact]
	public void ToDto_WithApprovedForRelease_MapsCorrectly()
	{
		// Arrange
		var issue = CreateTestIssue();
		issue.ApprovedForRelease = true;

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.ApprovedForRelease.Should().BeTrue();
	}

	[Fact]
	public void ToDto_WithRejected_MapsCorrectly()
	{
		// Arrange
		var issue = CreateTestIssue();
		issue.Rejected = true;

		// Act
		var result = IssueMapper.ToDto(issue);

		// Assert
		result.Rejected.Should().BeTrue();
	}

	#endregion

	#region ToModel Tests

	[Fact]
	public void ToModel_WithValidDto_ReturnsCorrectModel()
	{
		// Arrange
		var dto = CreateTestIssueDto();

		// Act
		var result = IssueMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(dto.Id);
		result.Title.Should().Be(dto.Title);
		result.Description.Should().Be(dto.Description);
		result.DateCreated.Should().Be(dto.DateCreated);
		result.DateModified.Should().Be(dto.DateModified);
		result.Archived.Should().Be(dto.Archived);
		result.ApprovedForRelease.Should().Be(dto.ApprovedForRelease);
		result.Rejected.Should().Be(dto.Rejected);
	}

	[Fact]
	public void ToModel_WithNullDto_ReturnsEmptyModel()
	{
		// Arrange
		IssueDto? dto = null;

		// Act
		var result = IssueMapper.ToModel(dto);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(ObjectId.Empty);
		result.Title.Should().BeEmpty();
		result.Description.Should().BeEmpty();
	}

	[Fact]
	public void ToModel_WithAuthor_MapsAuthorToUserInfo()
	{
		// Arrange
		var dto = CreateTestIssueDto() with
		{
			Author = new UserDto("auth0|123", "Test Author", "author@example.com")
		};

		// Act
		var result = IssueMapper.ToModel(dto);

		// Assert
		result.Author.Should().NotBeNull();
		result.Author.Id.Should().Be("auth0|123");
		result.Author.Name.Should().Be("Test Author");
		result.Author.Email.Should().Be("author@example.com");
	}

	[Fact]
	public void ToModel_WithCategory_MapsCategoryToCategoryInfo()
	{
		// Arrange
		var categoryId = ObjectId.GenerateNewId();
		var dto = CreateTestIssueDto() with
		{
			Category = new CategoryDto(
				categoryId,
				"Feature",
				"Feature category",
				DateTime.UtcNow,
				null,
				false,
				UserDto.Empty)
		};

		// Act
		var result = IssueMapper.ToModel(dto);

		// Assert
		result.Category.Should().NotBeNull();
		result.Category.Id.Should().Be(categoryId);
		result.Category.CategoryName.Should().Be("Feature");
		result.Category.CategoryDescription.Should().Be("Feature category");
	}

	[Fact]
	public void ToModel_WithStatus_MapsStatusToStatusInfo()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var dto = CreateTestIssueDto() with
		{
			Status = new StatusDto(
				statusId,
				"In Progress",
				"Work in progress",
				DateTime.UtcNow,
				null,
				false,
				UserDto.Empty)
		};

		// Act
		var result = IssueMapper.ToModel(dto);

		// Assert
		result.Status.Should().NotBeNull();
		result.Status.Id.Should().Be(statusId);
		result.Status.StatusName.Should().Be("In Progress");
		result.Status.StatusDescription.Should().Be("Work in progress");
	}

	[Fact]
	public void ToModel_WithArchivedBy_MapsArchivedByToUserInfo()
	{
		// Arrange
		var dto = CreateTestIssueDto() with
		{
			Archived = true,
			ArchivedBy = new UserDto("auth0|456", "Admin User", "admin@example.com")
		};

		// Act
		var result = IssueMapper.ToModel(dto);

		// Assert
		result.Archived.Should().BeTrue();
		result.ArchivedBy.Should().NotBeNull();
		result.ArchivedBy.Id.Should().Be("auth0|456");
		result.ArchivedBy.Name.Should().Be("Admin User");
	}

	[Fact]
	public void ToModel_WithApprovedForRelease_MapsCorrectly()
	{
		// Arrange
		var dto = CreateTestIssueDto() with { ApprovedForRelease = true };

		// Act
		var result = IssueMapper.ToModel(dto);

		// Assert
		result.ApprovedForRelease.Should().BeTrue();
	}

	[Fact]
	public void ToModel_WithRejected_MapsCorrectly()
	{
		// Arrange
		var dto = CreateTestIssueDto() with { Rejected = true };

		// Act
		var result = IssueMapper.ToModel(dto);

		// Assert
		result.Rejected.Should().BeTrue();
	}

	#endregion

	#region ToDtoList Tests

	[Fact]
	public void ToDtoList_WithValidIssues_ReturnsCorrectList()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue("Issue 1"),
			CreateTestIssue("Issue 2"),
			CreateTestIssue("Issue 3")
		};

		// Act
		var result = IssueMapper.ToDtoList(issues);

		// Assert
		result.Should().HaveCount(3);
		result[0].Title.Should().Be("Issue 1");
		result[1].Title.Should().Be("Issue 2");
		result[2].Title.Should().Be("Issue 3");
	}

	[Fact]
	public void ToDtoList_WithNullCollection_ReturnsEmptyList()
	{
		// Arrange
		IEnumerable<Issue>? issues = null;

		// Act
		var result = IssueMapper.ToDtoList(issues);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_WithEmptyCollection_ReturnsEmptyList()
	{
		// Arrange
		var issues = new List<Issue>();

		// Act
		var result = IssueMapper.ToDtoList(issues);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ToDtoList_PreservesAllProperties()
	{
		// Arrange
		var issues = new List<Issue>
		{
			CreateTestIssue("Test Issue")
		};
		issues[0].Archived = true;
		issues[0].ApprovedForRelease = true;
		issues[0].Rejected = false;

		// Act
		var result = IssueMapper.ToDtoList(issues);

		// Assert
		result.Should().HaveCount(1);
		result[0].Archived.Should().BeTrue();
		result[0].ApprovedForRelease.Should().BeTrue();
		result[0].Rejected.Should().BeFalse();
	}

	#endregion

	#region RoundTrip Tests

	[Fact]
	public void RoundTrip_ToDto_ThenToModel_PreservesData()
	{
		// Arrange
		var originalIssue = CreateTestIssue();
		originalIssue.Title = "Round Trip Test";
		originalIssue.Description = "Testing round trip conversion";
		originalIssue.Archived = true;
		originalIssue.ApprovedForRelease = true;
		originalIssue.Rejected = false;
		originalIssue.Author = new UserInfo { Id = "user1", Name = "Author", Email = "a@test.com" };
		originalIssue.Category = new CategoryInfo { Id = ObjectId.GenerateNewId(), CategoryName = "Bug", CategoryDescription = "Bug desc" };
		originalIssue.Status = new StatusInfo { Id = ObjectId.GenerateNewId(), StatusName = "Open", StatusDescription = "Open desc" };

		// Act
		var dto = IssueMapper.ToDto(originalIssue);
		var resultModel = IssueMapper.ToModel(dto);

		// Assert
		resultModel.Id.Should().Be(originalIssue.Id);
		resultModel.Title.Should().Be(originalIssue.Title);
		resultModel.Description.Should().Be(originalIssue.Description);
		resultModel.Archived.Should().Be(originalIssue.Archived);
		resultModel.ApprovedForRelease.Should().Be(originalIssue.ApprovedForRelease);
		resultModel.Rejected.Should().Be(originalIssue.Rejected);
		resultModel.Author.Id.Should().Be(originalIssue.Author.Id);
		resultModel.Category.CategoryName.Should().Be(originalIssue.Category.CategoryName);
		resultModel.Status.StatusName.Should().Be(originalIssue.Status.StatusName);
	}

	#endregion

	#region Helper Methods

	private static Issue CreateTestIssue(string title = "Test Issue")
	{
		return new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = title,
			Description = "Test Description",
			DateCreated = DateTime.UtcNow,
			DateModified = DateTime.UtcNow.AddHours(1),
			Author = UserInfo.Empty,
			Category = CategoryInfo.Empty,
			Status = StatusInfo.Empty,
			Archived = false,
			ArchivedBy = UserInfo.Empty,
			ApprovedForRelease = false,
			Rejected = false
		};
	}

	private static IssueDto CreateTestIssueDto(string title = "Test Issue DTO")
	{
		return new IssueDto(
			ObjectId.GenerateNewId(),
			title,
			"Test Description",
			DateTime.UtcNow,
			DateTime.UtcNow.AddHours(1),
			UserDto.Empty,
			CategoryDto.Empty,
			StatusDto.Empty,
			false,
			UserDto.Empty,
			false,
			false,
			UserDto.Empty,
			0,
			[],
			[]);
	}

	#endregion
}
