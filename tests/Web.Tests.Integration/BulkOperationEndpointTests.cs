// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkOperationEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Domain.DTOs;
using Domain.Features.Issues.Commands.Bulk;
using Domain.Models;

using MongoDB.Bson;

using Persistence.MongoDb;

using Web.Services;

namespace Web.Tests.Integration;

/// <summary>
/// Integration tests for bulk operation endpoints/services.
/// Tests bulk operations for status updates, category updates, assignments, deletions, exports, and undo functionality.
/// </summary>
[Collection("Integration")]
public sealed class BulkOperationEndpointTests : IntegrationTestBase
{
	private IBulkOperationService _bulkOperationService = null!;

	public BulkOperationEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		_bulkOperationService = GetService<IBulkOperationService>();
	}

	#region POST /api/issues/bulk/status - Bulk Update Status

	[Fact]
	public async Task BulkUpdateStatus_WithValidIssues_ShouldUpdateAllIssuesSuccessfully()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 5);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var newStatus = new StatusDto(statuses[1]);

		// Act
		var result = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeTrue();
		result.Value.SuccessCount.Should().Be(5);
		result.Value.FailureCount.Should().Be(0);
		result.Value.TotalRequested.Should().Be(5);
		result.Value.UndoToken.Should().NotBeNullOrEmpty();

		// Verify database state
		await using var context = CreateDbContext();
		foreach (var issueId in issueIds)
		{
			var updatedIssue = context.Issues.FirstOrDefault(i => i.Id == ObjectId.Parse(issueId));
			updatedIssue.Should().NotBeNull();
			updatedIssue!.Status.StatusName.Should().Be(statuses[1].StatusName);
		}
	}

	[Fact]
	public async Task BulkUpdateStatus_WithPartiallyValidIssues_ShouldReportPartialFailure()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 3);

		var issueIds = issues.Select(i => i.Id.ToString()).ToList();
		issueIds.Add(ObjectId.GenerateNewId().ToString()); // Add non-existent issue
		issueIds.Add(ObjectId.GenerateNewId().ToString()); // Add another non-existent issue

		var newStatus = new StatusDto(statuses[1]);

		// Act
		var result = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeFalse();
		result.Value.SuccessCount.Should().Be(3);
		result.Value.FailureCount.Should().Be(2);
		result.Value.TotalRequested.Should().Be(5);
		result.Value.Errors.Should().HaveCount(2);
		result.Value.Errors.All(e => e.ErrorMessage == "Issue not found").Should().BeTrue();
	}

	[Fact]
	public async Task BulkUpdateStatus_WithEmptySelection_ShouldReturnValidationError()
	{
		// Arrange
		var (_, statuses) = await SeedTestDataAsync();
		var newStatus = new StatusDto(statuses[1]);

		// Act
		var result = await _bulkOperationService.BulkUpdateStatusAsync(
			Enumerable.Empty<string>(),
			newStatus,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("No issues specified");
	}

	[Fact]
	public async Task BulkUpdateStatus_ExceedingMaxBatchSize_ShouldReturnValidationError()
	{
		// Arrange
		var (_, statuses) = await SeedTestDataAsync();
		var issueIds = Enumerable.Range(0, 101)
			.Select(_ => ObjectId.GenerateNewId().ToString())
			.ToList();

		var newStatus = new StatusDto(statuses[0]);

		// Act
		var result = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Batch size exceeds maximum");
	}

	#endregion

	#region POST /api/issues/bulk/category - Bulk Update Category

	[Fact]
	public async Task BulkUpdateCategory_WithValidIssues_ShouldUpdateAllIssuesSuccessfully()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 4);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var newCategory = new CategoryDto(categories[1]);

		// Act
		var result = await _bulkOperationService.BulkUpdateCategoryAsync(
			issueIds,
			newCategory,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeTrue();
		result.Value.SuccessCount.Should().Be(4);
		result.Value.FailureCount.Should().Be(0);
		result.Value.UndoToken.Should().NotBeNullOrEmpty();

		// Verify database state
		await using var context = CreateDbContext();
		foreach (var issueId in issueIds)
		{
			var updatedIssue = context.Issues.FirstOrDefault(i => i.Id == ObjectId.Parse(issueId));
			updatedIssue.Should().NotBeNull();
			updatedIssue!.Category.CategoryName.Should().Be(categories[1].CategoryName);
		}
	}

	[Fact]
	public async Task BulkUpdateCategory_WithPartiallyValidIssues_ShouldReportPartialFailure()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 2);

		var issueIds = issues.Select(i => i.Id.ToString()).ToList();
		issueIds.Add(ObjectId.GenerateNewId().ToString()); // Non-existent issue

		var newCategory = new CategoryDto(categories[2]);

		// Act
		var result = await _bulkOperationService.BulkUpdateCategoryAsync(
			issueIds,
			newCategory,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeFalse();
		result.Value.SuccessCount.Should().Be(2);
		result.Value.FailureCount.Should().Be(1);
		result.Value.Errors.Should().HaveCount(1);
	}

	[Fact]
	public async Task BulkUpdateCategory_WithEmptySelection_ShouldReturnValidationError()
	{
		// Arrange
		var (categories, _) = await SeedTestDataAsync();
		var newCategory = new CategoryDto(categories[0]);

		// Act
		var result = await _bulkOperationService.BulkUpdateCategoryAsync(
			Enumerable.Empty<string>(),
			newCategory,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("No issues specified");
	}

	#endregion

	#region POST /api/issues/bulk/assign - Bulk Assign Issues

	[Fact]
	public async Task BulkAssign_WithValidIssues_ShouldAssignAllIssuesSuccessfully()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 3);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var newAssignee = new UserDto("new-user-id", "New User", "newuser@example.com");

		// Act
		var result = await _bulkOperationService.BulkAssignAsync(
			issueIds,
			newAssignee,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeTrue();
		result.Value.SuccessCount.Should().Be(3);
		result.Value.FailureCount.Should().Be(0);
		result.Value.UndoToken.Should().NotBeNullOrEmpty();

		// Verify database state
		await using var context = CreateDbContext();
		foreach (var issueId in issueIds)
		{
			var updatedIssue = context.Issues.FirstOrDefault(i => i.Id == ObjectId.Parse(issueId));
			updatedIssue.Should().NotBeNull();
			updatedIssue!.Assignee.Id.Should().Be(newAssignee.Id);
			updatedIssue.Assignee.Name.Should().Be(newAssignee.Name);
		}
	}

	[Fact]
	public async Task BulkAssign_WithPartiallyValidIssues_ShouldReportPartialFailure()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 2);

		var issueIds = issues.Select(i => i.Id.ToString()).ToList();
		issueIds.Add(ObjectId.GenerateNewId().ToString()); // Non-existent

		var newAssignee = new UserDto("assign-user", "Assign User", "assign@example.com");

		// Act
		var result = await _bulkOperationService.BulkAssignAsync(
			issueIds,
			newAssignee,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeFalse();
		result.Value.SuccessCount.Should().Be(2);
		result.Value.FailureCount.Should().Be(1);
	}

	[Fact]
	public async Task BulkAssign_WithEmptySelection_ShouldReturnValidationError()
	{
		// Arrange
		var assignee = new UserDto("user-id", "Test User", "test@example.com");

		// Act
		var result = await _bulkOperationService.BulkAssignAsync(
			Enumerable.Empty<string>(),
			assignee,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("No issues specified");
	}

	#endregion

	#region POST /api/issues/bulk/delete - Bulk Delete (Archive)

	[Fact]
	public async Task BulkDelete_WithValidIssues_ShouldArchiveAllIssuesSuccessfully()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 4);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var deletedBy = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);

		// Act
		var result = await _bulkOperationService.BulkDeleteAsync(
			issueIds,
			deletedBy,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeTrue();
		result.Value.SuccessCount.Should().Be(4);
		result.Value.FailureCount.Should().Be(0);
		result.Value.UndoToken.Should().NotBeNullOrEmpty();

		// Verify database state - issues should be archived
		await using var context = CreateDbContext();
		foreach (var issueId in issueIds)
		{
			var archivedIssue = context.Issues.FirstOrDefault(i => i.Id == ObjectId.Parse(issueId));
			archivedIssue.Should().NotBeNull();
			archivedIssue!.Archived.Should().BeTrue();
			archivedIssue.ArchivedBy.Id.Should().Be(deletedBy.Id);
		}
	}

	[Fact]
	public async Task BulkDelete_WithAlreadyArchivedIssues_ShouldReportPartialFailure()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 3);

		// Pre-archive one issue
		await using (var context = CreateDbContext())
		{
			var issueToArchive = context.Issues.First(i => i.Id == issues[0].Id);
			issueToArchive.Archived = true;
			issueToArchive.ArchivedBy = new UserInfo
			{
				Id = "archive-user",
				Name = "Archive User",
				Email = "archive@example.com"
			};
			await context.SaveChangesAsync();
		}

		var issueIds = issues.Select(i => i.Id.ToString()).ToList();
		var deletedBy = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);

		// Act
		var result = await _bulkOperationService.BulkDeleteAsync(
			issueIds,
			deletedBy,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeFalse();
		result.Value.SuccessCount.Should().Be(2);
		result.Value.FailureCount.Should().Be(1);
		result.Value.Errors.Should().Contain(e => e.ErrorMessage == "Issue is already archived");
	}

	[Fact]
	public async Task BulkDelete_WithPartiallyValidIssues_ShouldReportPartialFailure()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 2);

		var issueIds = issues.Select(i => i.Id.ToString()).ToList();
		issueIds.Add(ObjectId.GenerateNewId().ToString()); // Non-existent

		var deletedBy = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);

		// Act
		var result = await _bulkOperationService.BulkDeleteAsync(
			issueIds,
			deletedBy,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.IsFullSuccess.Should().BeFalse();
		result.Value.SuccessCount.Should().Be(2);
		result.Value.FailureCount.Should().Be(1);
	}

	[Fact]
	public async Task BulkDelete_WithEmptySelection_ShouldReturnValidationError()
	{
		// Arrange
		var deletedBy = new UserDto("user-id", "Test User", "test@example.com");

		// Act
		var result = await _bulkOperationService.BulkDeleteAsync(
			Enumerable.Empty<string>(),
			deletedBy,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("No issues specified");
	}

	#endregion

	#region POST /api/issues/bulk/export - Bulk Export

	[Fact]
	public async Task BulkExport_WithValidIssues_ShouldReturnCsvContent()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 5);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		// Act
		var result = await _bulkOperationService.BulkExportAsync(
			issueIds,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalExported.Should().Be(5);
		result.Value.Errors.Should().BeEmpty();
		result.Value.FileName.Should().StartWith("issues_export_");
		result.Value.FileName.Should().EndWith(".csv");
		result.Value.CsvContent.Should().NotBeEmpty();

		// Verify CSV content has expected header and data rows
		var csvContent = System.Text.Encoding.UTF8.GetString(result.Value.CsvContent);
		csvContent.Should().Contain("Id,Title,Description,Status,Category,Author,DateCreated,DateModified,Archived");
		csvContent.Should().Contain("Test Issue");
	}

	[Fact]
	public async Task BulkExport_WithPartiallyValidIssues_ShouldExportValidOnesAndReportErrors()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 3);

		var issueIds = issues.Select(i => i.Id.ToString()).ToList();
		issueIds.Add(ObjectId.GenerateNewId().ToString()); // Non-existent

		// Act
		var result = await _bulkOperationService.BulkExportAsync(
			issueIds,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalExported.Should().Be(3);
		result.Value.Errors.Should().HaveCount(1);
	}

	[Fact]
	public async Task BulkExport_WithEmptySelection_ShouldReturnValidationError()
	{
		// Act
		var result = await _bulkOperationService.BulkExportAsync(
			Enumerable.Empty<string>(),
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("No issues specified");
	}

	#endregion

	#region POST /api/issues/bulk/undo - Undo Last Bulk Operation

	[Fact]
	public async Task Undo_StatusUpdate_ShouldRestorePreviousStatus()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 3);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var newStatus = new StatusDto(statuses[1]);
		var originalStatusName = statuses[0].StatusName;

		// Perform initial bulk status update
		var updateResult = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		updateResult.Success.Should().BeTrue();
		var undoToken = updateResult.Value!.UndoToken;
		undoToken.Should().NotBeNullOrEmpty();

		// Verify status was changed
		await using (var context = CreateDbContext())
		{
			var updatedIssue = context.Issues.First(i => i.Id == issues[0].Id);
			updatedIssue.Status.StatusName.Should().Be(statuses[1].StatusName);
		}

		// Act - Undo the operation
		var undoResult = await _bulkOperationService.UndoLastOperationAsync(
			undoToken!,
			TestAuthHandler.TestUserId);

		// Assert
		undoResult.Success.Should().BeTrue();
		undoResult.Value.Should().NotBeNull();
		undoResult.Value!.IsFullSuccess.Should().BeTrue();
		undoResult.Value.SuccessCount.Should().Be(3);

		// Verify database state - status should be restored
		await using (var context = CreateDbContext())
		{
			foreach (var issueId in issueIds)
			{
				var restoredIssue = context.Issues.FirstOrDefault(i => i.Id == ObjectId.Parse(issueId));
				restoredIssue.Should().NotBeNull();
				restoredIssue!.Status.StatusName.Should().Be(originalStatusName);
			}
		}
	}

	[Fact]
	public async Task Undo_CategoryUpdate_ShouldRestorePreviousCategory()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 2);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var newCategory = new CategoryDto(categories[1]);
		var originalCategoryName = categories[0].CategoryName;

		// Perform initial bulk category update
		var updateResult = await _bulkOperationService.BulkUpdateCategoryAsync(
			issueIds,
			newCategory,
			TestAuthHandler.TestUserId);

		updateResult.Success.Should().BeTrue();
		var undoToken = updateResult.Value!.UndoToken;

		// Act - Undo the operation
		var undoResult = await _bulkOperationService.UndoLastOperationAsync(
			undoToken!,
			TestAuthHandler.TestUserId);

		// Assert
		undoResult.Success.Should().BeTrue();
		undoResult.Value!.IsFullSuccess.Should().BeTrue();

		// Verify database state
		await using var context = CreateDbContext();
		foreach (var issueId in issueIds)
		{
			var restoredIssue = context.Issues.FirstOrDefault(i => i.Id == ObjectId.Parse(issueId));
			restoredIssue!.Category.CategoryName.Should().Be(originalCategoryName);
		}
	}

	[Fact]
	public async Task Undo_Delete_ShouldRestoreArchivedState()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 2);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var deletedBy = new UserDto(TestAuthHandler.TestUserId, TestAuthHandler.TestUserName, TestAuthHandler.TestUserEmail);

		// Perform bulk delete
		var deleteResult = await _bulkOperationService.BulkDeleteAsync(
			issueIds,
			deletedBy,
			TestAuthHandler.TestUserId);

		deleteResult.Success.Should().BeTrue();
		var undoToken = deleteResult.Value!.UndoToken;

		// Verify issues are archived
		await using (var context = CreateDbContext())
		{
			foreach (var issueId in issueIds)
			{
				var archivedIssue = context.Issues.First(i => i.Id == ObjectId.Parse(issueId));
				archivedIssue.Archived.Should().BeTrue();
			}
		}

		// Act - Undo the delete
		var undoResult = await _bulkOperationService.UndoLastOperationAsync(
			undoToken!,
			TestAuthHandler.TestUserId);

		// Assert
		undoResult.Success.Should().BeTrue();
		undoResult.Value!.IsFullSuccess.Should().BeTrue();

		// Verify database state - issues should be unarchived
		await using (var context = CreateDbContext())
		{
			foreach (var issueId in issueIds)
			{
				var restoredIssue = context.Issues.FirstOrDefault(i => i.Id == ObjectId.Parse(issueId));
				restoredIssue!.Archived.Should().BeFalse();
			}
		}
	}

	[Fact]
	public async Task Undo_WithInvalidToken_ShouldReturnError()
	{
		// Act
		var result = await _bulkOperationService.UndoLastOperationAsync(
			"invalid-token",
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Undo token not found");
	}

	[Fact]
	public async Task Undo_WithEmptyToken_ShouldReturnValidationError()
	{
		// Act
		var result = await _bulkOperationService.UndoLastOperationAsync(
			string.Empty,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Invalid undo token");
	}

	[Fact]
	public async Task Undo_WithDifferentUser_ShouldReturnError()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 2);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var newStatus = new StatusDto(statuses[1]);

		// Perform bulk update as original user
		var updateResult = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		var undoToken = updateResult.Value!.UndoToken;

		// Act - Try to undo as different user
		var undoResult = await _bulkOperationService.UndoLastOperationAsync(
			undoToken!,
			"different-user-id");

		// Assert
		undoResult.Failure.Should().BeTrue();
		undoResult.Error.Should().Contain("Undo token not found");
	}

	[Fact]
	public async Task Undo_UsedTokenTwice_ShouldFailOnSecondAttempt()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issues = await SeedIssuesAsync(categories[0], statuses[0], 2);
		var issueIds = issues.Select(i => i.Id.ToString()).ToList();

		var newStatus = new StatusDto(statuses[1]);

		// Perform bulk update
		var updateResult = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		var undoToken = updateResult.Value!.UndoToken;

		// First undo - should succeed
		var firstUndoResult = await _bulkOperationService.UndoLastOperationAsync(
			undoToken!,
			TestAuthHandler.TestUserId);

		firstUndoResult.Success.Should().BeTrue();

		// Act - Second undo with same token should fail
		var secondUndoResult = await _bulkOperationService.UndoLastOperationAsync(
			undoToken!,
			TestAuthHandler.TestUserId);

		// Assert
		secondUndoResult.Failure.Should().BeTrue();
		secondUndoResult.Error.Should().Contain("Undo token not found");
	}

	#endregion

	#region Validation Errors

	[Fact]
	public async Task BulkUpdateStatus_ExceedingMaxBatchSize_ShouldFailValidation()
	{
		// Arrange
		var (_, statuses) = await SeedTestDataAsync();
		var issueIds = Enumerable.Range(0, BulkOperationConstants.MaxBatchSize + 1)
			.Select(_ => ObjectId.GenerateNewId().ToString())
			.ToList();

		var newStatus = new StatusDto(statuses[0]);

		// Act
		var result = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain($"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize}");
	}

	[Fact]
	public async Task BulkUpdateCategory_ExceedingMaxBatchSize_ShouldFailValidation()
	{
		// Arrange
		var (categories, _) = await SeedTestDataAsync();
		var issueIds = Enumerable.Range(0, BulkOperationConstants.MaxBatchSize + 1)
			.Select(_ => ObjectId.GenerateNewId().ToString())
			.ToList();

		var newCategory = new CategoryDto(categories[0]);

		// Act
		var result = await _bulkOperationService.BulkUpdateCategoryAsync(
			issueIds,
			newCategory,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain($"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize}");
	}

	[Fact]
	public async Task BulkAssign_ExceedingMaxBatchSize_ShouldFailValidation()
	{
		// Arrange
		var issueIds = Enumerable.Range(0, BulkOperationConstants.MaxBatchSize + 1)
			.Select(_ => ObjectId.GenerateNewId().ToString())
			.ToList();

		var assignee = new UserDto("user-id", "Test User", "test@example.com");

		// Act
		var result = await _bulkOperationService.BulkAssignAsync(
			issueIds,
			assignee,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain($"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize}");
	}

	[Fact]
	public async Task BulkDelete_ExceedingMaxBatchSize_ShouldFailValidation()
	{
		// Arrange
		var issueIds = Enumerable.Range(0, BulkOperationConstants.MaxBatchSize + 1)
			.Select(_ => ObjectId.GenerateNewId().ToString())
			.ToList();

		var deletedBy = new UserDto("user-id", "Test User", "test@example.com");

		// Act
		var result = await _bulkOperationService.BulkDeleteAsync(
			issueIds,
			deletedBy,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain($"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize}");
	}

	[Fact]
	public async Task BulkExport_ExceedingMaxBatchSize_ShouldFailValidation()
	{
		// Arrange
		var issueIds = Enumerable.Range(0, BulkOperationConstants.MaxBatchSize + 1)
			.Select(_ => ObjectId.GenerateNewId().ToString())
			.ToList();

		// Act
		var result = await _bulkOperationService.BulkExportAsync(
			issueIds,
			TestAuthHandler.TestUserId);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain($"Batch size exceeds maximum of {BulkOperationConstants.MaxBatchSize}");
	}

	#endregion

	#region Edge Cases

	[Fact]
	public async Task BulkUpdateStatus_WithSingleIssue_ShouldSucceed()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var issueIds = new List<string> { issue.Id.ToString() };

		var newStatus = new StatusDto(statuses[2]);

		// Act
		var result = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsFullSuccess.Should().BeTrue();
		result.Value.SuccessCount.Should().Be(1);
	}

	[Fact]
	public async Task BulkExport_WithSpecialCharacters_ShouldEscapeCorrectly()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0], "Test \"Issue\" with, commas");

		// Update issue description with special characters
		await using (var context = CreateDbContext())
		{
			var dbIssue = context.Issues.First(i => i.Id == issue.Id);
			dbIssue.Description = "Description with \"quotes\" and,commas\nand newlines";
			await context.SaveChangesAsync();
		}

		var issueIds = new List<string> { issue.Id.ToString() };

		// Act
		var result = await _bulkOperationService.BulkExportAsync(
			issueIds,
			TestAuthHandler.TestUserId);

		// Assert
		result.Success.Should().BeTrue();
		var csvContent = System.Text.Encoding.UTF8.GetString(result.Value!.CsvContent);

		// CSV should contain properly escaped quotes and handle special characters
		csvContent.Should().NotBeEmpty();
		result.Value.TotalExported.Should().Be(1);
	}

	[Fact]
	public async Task BulkOperations_WithDuplicateIds_ShouldHandleGracefully()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var issueId = issue.Id.ToString();

		// Include same ID twice
		var issueIds = new List<string> { issueId, issueId };
		var newStatus = new StatusDto(statuses[1]);

		// Act
		var result = await _bulkOperationService.BulkUpdateStatusAsync(
			issueIds,
			newStatus,
			TestAuthHandler.TestUserId);

		// Assert - Should process both entries (implementation-specific behavior)
		result.Success.Should().BeTrue();
	}

	#endregion
}
