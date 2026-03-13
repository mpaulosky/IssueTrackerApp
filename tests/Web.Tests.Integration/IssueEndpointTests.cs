// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Models;
using MongoDB.Bson;
using Web.Services;

namespace Web.Tests.Integration;

/// <summary>
/// Integration tests for Issue service operations.
/// Tests the IIssueService layer which powers issue management functionality.
/// Uses Testcontainers MongoDB for real database testing.
/// </summary>
public sealed class IssueEndpointTests : IntegrationTestBase
{
	public IssueEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region GET Issues (List with Pagination)

	[Fact]
	public async Task GetIssues_WithNoIssues_ReturnsEmptyList()
	{
		// Arrange
		await SeedTestDataAsync();
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssuesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().BeEmpty();
		result.Value.Total.Should().Be(0);
	}

	[Fact]
	public async Task GetIssues_WithMultipleIssues_ReturnsPagedResults()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 15);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(10);
		result.Value.Total.Should().Be(15);
		result.Value.Page.Should().Be(1);
		result.Value.PageSize.Should().Be(10);
		result.Value.TotalPages.Should().Be(2);
	}

	[Fact]
	public async Task GetIssues_WithPagination_ReturnsCorrectPage()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 25);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssuesAsync(page: 2, pageSize: 10);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(10);
		result.Value.Page.Should().Be(2);
		result.Value.Total.Should().Be(25);
	}

	[Fact]
	public async Task GetIssues_LastPage_ReturnsRemainingItems()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 25);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssuesAsync(page: 3, pageSize: 10);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(5);
		result.Value.Page.Should().Be(3);
	}

	[Fact]
	public async Task GetIssues_WithStatusFilter_ReturnsFilteredResults()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 5); // Open issues
		await SeedIssuesAsync(categories[0], statuses[1], 3); // In Progress issues
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssuesAsync(statusFilter: "Open");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(5);
		result.Value.Items.Should().AllSatisfy(i => i.Status.StatusName.Should().Be("Open"));
	}

	[Fact]
	public async Task GetIssues_WithCategoryFilter_ReturnsFilteredResults()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 4); // Bug category
		await SeedIssuesAsync(categories[1], statuses[0], 6); // Feature category
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssuesAsync(categoryFilter: "Bug");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(4);
		result.Value.Items.Should().AllSatisfy(i => i.Category.CategoryName.Should().Be("Bug"));
	}

	[Fact]
	public async Task GetIssues_ExcludesArchivedByDefault()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueAsync(categories[0], statuses[0], "Active Issue");
		var archivedIssue = await SeedIssueAsync(categories[0], statuses[0], "Archived Issue");

		// Archive the second issue
		await using var context = CreateDbContext();
		var issue = await context.Issues.FindAsync(archivedIssue.Id);
		issue!.Archived = true;
		await context.SaveChangesAsync();

		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssuesAsync(includeArchived: false);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(1);
		result.Value.Items.First().Title.Should().Be("Active Issue");
	}

	[Fact]
	public async Task GetIssues_IncludesArchivedWhenRequested()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueAsync(categories[0], statuses[0], "Active Issue");
		var archivedIssue = await SeedIssueAsync(categories[0], statuses[0], "Archived Issue");

		// Archive the second issue
		await using var context = CreateDbContext();
		var issue = await context.Issues.FindAsync(archivedIssue.Id);
		issue!.Archived = true;
		await context.SaveChangesAsync();

		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssuesAsync(includeArchived: true);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(2);
	}

	#endregion

	#region GET Issue by ID (Single Issue)

	[Fact]
	public async Task GetIssueById_WithValidId_ReturnsIssue()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Test Issue");
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssueByIdAsync(seededIssue.Id.ToString());

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().Be("Test Issue");
		result.Value.Description.Should().Be("Test issue description");
		result.Value.Category.CategoryName.Should().Be("Bug");
		result.Value.Status.StatusName.Should().Be("Open");
	}

	[Fact]
	public async Task GetIssueById_WithInvalidId_ReturnsNotFound()
	{
		// Arrange
		await SeedTestDataAsync();
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssueByIdAsync(nonExistentId);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task GetIssueById_WithMalformedId_ReturnsNotFound()
	{
		// Arrange
		await SeedTestDataAsync();
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.GetIssueByIdAsync("invalid-id-format");

		// Assert
		result.Failure.Should().BeTrue();
	}

	#endregion

	#region POST Issue (Create)

	[Fact]
	public async Task CreateIssue_WithValidData_ReturnsCreatedIssue()
	{
		// Arrange
		var (categories, _) = await SeedTestDataAsync();
		var categoryDto = new CategoryDto(categories[0]);
		var author = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.CreateIssueAsync(
			"New Bug Report",
			"This is a detailed bug description",
			categoryDto,
			author);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().Be("New Bug Report");
		result.Value.Description.Should().Be("This is a detailed bug description");
		result.Value.Category.CategoryName.Should().Be("Bug");
		result.Value.Author.Id.Should().Be(TestAuthHandler.TestUserId);
		result.Value.Id.Should().NotBe(ObjectId.Empty);
	}

	[Fact]
	public async Task CreateIssue_WithEmptyTitle_ReturnsValidationError()
	{
		// Arrange
		var (categories, _) = await SeedTestDataAsync();
		var categoryDto = new CategoryDto(categories[0]);
		var author = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.CreateIssueAsync(
			"",
			"Description",
			categoryDto,
			author);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task CreateIssue_WithEmptyDescription_ReturnsValidationError()
	{
		// Arrange
		var (categories, _) = await SeedTestDataAsync();
		var categoryDto = new CategoryDto(categories[0]);
		var author = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.CreateIssueAsync(
			"Valid Title",
			"",
			categoryDto,
			author);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task CreateIssue_SetsDefaultStatus()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var categoryDto = new CategoryDto(categories[0]);
		var author = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.CreateIssueAsync(
			"New Issue",
			"Description",
			categoryDto,
			author);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Status.StatusName.Should().Be("Open");
	}

	[Fact]
	public async Task CreateIssue_PersistsToDatabase()
	{
		// Arrange
		var (categories, _) = await SeedTestDataAsync();
		var categoryDto = new CategoryDto(categories[0]);
		var author = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		var createResult = await issueService.CreateIssueAsync(
			"Persisted Issue",
			"Should be in database",
			categoryDto,
			author);

		// Assert - Verify by reading back from database
		var getResult = await issueService.GetIssueByIdAsync(createResult.Value!.Id.ToString());
		getResult.Success.Should().BeTrue();
		getResult.Value!.Title.Should().Be("Persisted Issue");
	}

	#endregion

	#region PUT Issue (Update)

	[Fact]
	public async Task UpdateIssue_WithValidData_ReturnsUpdatedIssue()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Original Title");
		var newCategoryDto = new CategoryDto(categories[1]); // Change to Feature
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.UpdateIssueAsync(
			seededIssue.Id.ToString(),
			"Updated Title",
			"Updated description",
			newCategoryDto);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().Be("Updated Title");
		result.Value.Description.Should().Be("Updated description");
		result.Value.Category.CategoryName.Should().Be("Feature");
		result.Value.DateModified.Should().NotBeNull();
	}

	[Fact]
	public async Task UpdateIssue_WithNonExistentId_ReturnsNotFound()
	{
		// Arrange
		var (categories, _) = await SeedTestDataAsync();
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		var categoryDto = new CategoryDto(categories[0]);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.UpdateIssueAsync(
			nonExistentId,
			"Updated Title",
			"Updated description",
			categoryDto);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task UpdateIssue_WithEmptyTitle_ReturnsValidationError()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Original Title");
		var categoryDto = new CategoryDto(categories[0]);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.UpdateIssueAsync(
			seededIssue.Id.ToString(),
			"",
			"Updated description",
			categoryDto);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task UpdateIssue_PersistsChanges()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Original Title");
		var categoryDto = new CategoryDto(categories[0]);
		var issueService = GetService<IIssueService>();

		// Act
		await issueService.UpdateIssueAsync(
			seededIssue.Id.ToString(),
			"Persisted Update",
			"Persisted description",
			categoryDto);

		// Assert - Verify by reading back
		var getResult = await issueService.GetIssueByIdAsync(seededIssue.Id.ToString());
		getResult.Value!.Title.Should().Be("Persisted Update");
		getResult.Value.Description.Should().Be("Persisted description");
	}

	#endregion

	#region DELETE Issue (Soft Delete / Archive)

	[Fact]
	public async Task DeleteIssue_WithValidId_ArchivesIssue()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Issue to Delete");
		var archivedBy = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.DeleteIssueAsync(seededIssue.Id.ToString(), archivedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteIssue_SetsArchivedFlag()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Issue to Archive");
		var archivedBy = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		await issueService.DeleteIssueAsync(seededIssue.Id.ToString(), archivedBy);

		// Assert - Verify the issue is archived in database
		await using var context = CreateDbContext();
		var issue = await context.Issues.FindAsync(seededIssue.Id);
		issue!.Archived.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteIssue_SetsArchivedByUser()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Issue to Archive");
		var archivedBy = new UserDto("admin-user-id", "Admin User", "admin@example.com");
		var issueService = GetService<IIssueService>();

		// Act
		await issueService.DeleteIssueAsync(seededIssue.Id.ToString(), archivedBy);

		// Assert - Verify the archived by user is set
		await using var context = CreateDbContext();
		var issue = await context.Issues.FindAsync(seededIssue.Id);
		issue!.ArchivedBy.Id.Should().Be("admin-user-id");
		issue.ArchivedBy.Name.Should().Be("Admin User");
	}

	[Fact]
	public async Task DeleteIssue_WithNonExistentId_ReturnsNotFound()
	{
		// Arrange
		await SeedTestDataAsync();
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		var archivedBy = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.DeleteIssueAsync(nonExistentId, archivedBy);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task DeleteIssue_ExcludesFromDefaultList()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Issue to Hide");
		var archivedBy = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);
		var issueService = GetService<IIssueService>();

		// Act
		await issueService.DeleteIssueAsync(seededIssue.Id.ToString(), archivedBy);

		// Assert - Issue should not appear in default list
		var listResult = await issueService.GetIssuesAsync(includeArchived: false);
		listResult.Value!.Items.Should().NotContain(i => i.Id == seededIssue.Id);
	}

	#endregion

	#region Search Issues

	[Fact]
	public async Task SearchIssues_WithSearchText_ReturnsMatchingIssues()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueAsync(categories[0], statuses[0], "Login Bug");
		await SeedIssueAsync(categories[0], statuses[0], "Dashboard Error");
		await SeedIssueAsync(categories[0], statuses[0], "Login Page Crash");
		var issueService = GetService<IIssueService>();

		var searchRequest = new IssueSearchRequest
		{
			SearchText = "Login",
			Page = 1,
			PageSize = 20
		};

		// Act
		var result = await issueService.SearchIssuesAsync(searchRequest);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(2);
		result.Value.Items.Should().AllSatisfy(i => i.Title.Should().Contain("Login"));
	}

	[Fact]
	public async Task SearchIssues_WithStatusFilter_ReturnsFilteredResults()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 3); // Open
		await SeedIssuesAsync(categories[0], statuses[1], 2); // In Progress
		var issueService = GetService<IIssueService>();

		var searchRequest = new IssueSearchRequest
		{
			StatusFilter = "In Progress",
			Page = 1,
			PageSize = 20
		};

		// Act
		var result = await issueService.SearchIssuesAsync(searchRequest);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(2);
		result.Value.Items.Should().AllSatisfy(i => i.Status.StatusName.Should().Be("In Progress"));
	}

	[Fact]
	public async Task SearchIssues_WithCategoryFilter_ReturnsFilteredResults()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 4); // Bug
		await SeedIssuesAsync(categories[1], statuses[0], 3); // Feature
		var issueService = GetService<IIssueService>();

		var searchRequest = new IssueSearchRequest
		{
			CategoryFilter = "Feature",
			Page = 1,
			PageSize = 20
		};

		// Act
		var result = await issueService.SearchIssuesAsync(searchRequest);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(3);
		result.Value.Items.Should().AllSatisfy(i => i.Category.CategoryName.Should().Be("Feature"));
	}

	[Fact]
	public async Task SearchIssues_WithMultipleFilters_ReturnsCombinedResults()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueAsync(categories[0], statuses[0], "Open Bug 1");
		await SeedIssueAsync(categories[0], statuses[1], "InProgress Bug 1");
		await SeedIssueAsync(categories[1], statuses[0], "Open Feature 1");
		var issueService = GetService<IIssueService>();

		var searchRequest = new IssueSearchRequest
		{
			StatusFilter = "Open",
			CategoryFilter = "Bug",
			Page = 1,
			PageSize = 20
		};

		// Act
		var result = await issueService.SearchIssuesAsync(searchRequest);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(1);
		result.Value.Items.First().Title.Should().Be("Open Bug 1");
	}

	[Fact]
	public async Task SearchIssues_WithPagination_ReturnsCorrectPage()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 30);
		var issueService = GetService<IIssueService>();

		var searchRequest = new IssueSearchRequest
		{
			Page = 2,
			PageSize = 10
		};

		// Act
		var result = await issueService.SearchIssuesAsync(searchRequest);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(10);
		result.Value.Page.Should().Be(2);
		result.Value.TotalCount.Should().Be(30);
		result.Value.TotalPages.Should().Be(3);
		result.Value.HasPreviousPage.Should().BeTrue();
		result.Value.HasNextPage.Should().BeTrue();
	}

	[Fact]
	public async Task SearchIssues_ExcludesArchivedByDefault()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueAsync(categories[0], statuses[0], "Active Issue");
		var archivedIssue = await SeedIssueAsync(categories[0], statuses[0], "Archived Issue");

		// Archive the second issue
		await using var context = CreateDbContext();
		var issue = await context.Issues.FindAsync(archivedIssue.Id);
		issue!.Archived = true;
		await context.SaveChangesAsync();

		var issueService = GetService<IIssueService>();

		var searchRequest = new IssueSearchRequest
		{
			IncludeArchived = false,
			Page = 1,
			PageSize = 20
		};

		// Act
		var result = await issueService.SearchIssuesAsync(searchRequest);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(1);
		result.Value.Items.First().Title.Should().Be("Active Issue");
	}

	[Fact]
	public async Task SearchIssues_IncludesArchivedWhenRequested()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueAsync(categories[0], statuses[0], "Active Issue");
		var archivedIssue = await SeedIssueAsync(categories[0], statuses[0], "Archived Issue");

		// Archive the second issue
		await using var context = CreateDbContext();
		var issue = await context.Issues.FindAsync(archivedIssue.Id);
		issue!.Archived = true;
		await context.SaveChangesAsync();

		var issueService = GetService<IIssueService>();

		var searchRequest = new IssueSearchRequest
		{
			IncludeArchived = true,
			Page = 1,
			PageSize = 20
		};

		// Act
		var result = await issueService.SearchIssuesAsync(searchRequest);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().HaveCount(2);
	}

	[Fact]
	public async Task SearchIssues_WithNoMatches_ReturnsEmptyResult()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssueAsync(categories[0], statuses[0], "Bug Report");
		var issueService = GetService<IIssueService>();

		var searchRequest = new IssueSearchRequest
		{
			SearchText = "NonExistentText12345",
			Page = 1,
			PageSize = 20
		};

		// Act
		var result = await issueService.SearchIssuesAsync(searchRequest);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Items.Should().BeEmpty();
		result.Value.TotalCount.Should().Be(0);
	}

	#endregion

	#region Change Issue Status

	[Fact]
	public async Task ChangeIssueStatus_WithValidData_UpdatesStatus()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Issue to Update Status");
		var newStatusDto = new StatusDto(statuses[1]); // Change to In Progress
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.ChangeIssueStatusAsync(
			seededIssue.Id.ToString(),
			newStatusDto);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Status.StatusName.Should().Be("In Progress");
		result.Value.DateModified.Should().NotBeNull();
	}

	[Fact]
	public async Task ChangeIssueStatus_WithNonExistentId_ReturnsNotFound()
	{
		// Arrange
		var (_, statuses) = await SeedTestDataAsync();
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		var statusDto = new StatusDto(statuses[0]);
		var issueService = GetService<IIssueService>();

		// Act
		var result = await issueService.ChangeIssueStatusAsync(nonExistentId, statusDto);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task ChangeIssueStatus_PersistsChange()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var seededIssue = await SeedIssueAsync(categories[0], statuses[0], "Issue to Close");
		var closedStatusDto = new StatusDto(statuses[2]); // Change to Closed
		var issueService = GetService<IIssueService>();

		// Act
		await issueService.ChangeIssueStatusAsync(seededIssue.Id.ToString(), closedStatusDto);

		// Assert - Verify by reading back
		var getResult = await issueService.GetIssueByIdAsync(seededIssue.Id.ToString());
		getResult.Value!.Status.StatusName.Should().Be("Closed");
	}

	#endregion
}
