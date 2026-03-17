// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Domain.DTOs;
using Domain.Models;
using MongoDB.Bson;
using Web.Features;

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests for Comment API endpoints.
/// </summary>
[Collection("Integration")]
public sealed class CommentEndpointTests : IntegrationTestBase
{
	public CommentEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region Helper Methods

	/// <summary>
	/// Seeds a test comment into the database.
	/// </summary>
	/// <param name="issue">The issue to attach the comment to.</param>
	/// <param name="title">Optional title override.</param>
	/// <param name="authorId">Optional author ID override.</param>
	/// <returns>The seeded comment.</returns>
	private async Task<Comment> SeedCommentAsync(
		Issue issue,
		string title = "Test Comment",
		string? authorId = null)
	{
		await using var context = CreateDbContext();

		var comment = new Comment
		{
			Id = ObjectId.GenerateNewId(),
			Title = title,
			Description = "Test comment description",
			IssueId = issue.Id,
			Author = new UserInfo
			{
				Id = authorId ?? TestAuthHandler.TestUserId,
				Name = TestAuthHandler.TestUserName,
				Email = TestAuthHandler.TestUserEmail
			}
		};

		context.Comments.Add(comment);
		await context.SaveChangesAsync();

		return comment;
	}

	/// <summary>
	/// Seeds multiple test comments into the database.
	/// </summary>
	/// <param name="issue">The issue to attach the comments to.</param>
	/// <param name="count">Number of comments to create.</param>
	/// <returns>The seeded comments.</returns>
	private async Task<List<Comment>> SeedCommentsAsync(Issue issue, int count)
	{
		await using var context = CreateDbContext();

		var comments = new List<Comment>();
		for (var i = 1; i <= count; i++)
		{
			comments.Add(new Comment
			{
				Id = ObjectId.GenerateNewId(),
				Title = $"Test Comment {i}",
				Description = $"Test comment description {i}",
				IssueId = issue.Id,
				Author = new UserInfo
				{
					Id = TestAuthHandler.TestUserId,
					Name = TestAuthHandler.TestUserName,
					Email = TestAuthHandler.TestUserEmail
				}
			});
		}

		context.Comments.AddRange(comments);
		await context.SaveChangesAsync();

		return comments;
	}

	#endregion

	#region GET /api/issues/{issueId}/comments - List Comments for Issue

	[Fact]
	public async Task GetComments_ReturnsEmptyList_WhenNoCommentsExist()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/issues/{issue.Id}/comments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var comments = await response.Content.ReadFromJsonAsync<List<CommentDto>>(JsonOptions);
		comments.Should().NotBeNull();
		comments.Should().BeEmpty();
	}

	[Fact]
	public async Task GetComments_ReturnsAllComments_WhenCommentsExist()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var seededComments = await SeedCommentsAsync(issue, 3);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/issues/{issue.Id}/comments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var comments = await response.Content.ReadFromJsonAsync<List<CommentDto>>(JsonOptions);
		comments.Should().NotBeNull();
		comments.Should().HaveCount(seededComments.Count);
		comments!.Select(c => c.Title).Should().Contain(seededComments.Select(c => c.Title));
	}

	[Fact]
	public async Task GetComments_ReturnsNotFound_WhenIssueDoesNotExist()
	{
		// Arrange
		var nonExistentIssueId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/issues/{nonExistentIssueId}/comments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetComments_ExcludesArchivedByDefault()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var seededComments = await SeedCommentsAsync(issue, 3);

		// Archive one comment
		await using var context = CreateDbContext();
		var commentToArchive = context.Comments.First();
		commentToArchive.Archived = true;
		await context.SaveChangesAsync();

		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/issues/{issue.Id}/comments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var comments = await response.Content.ReadFromJsonAsync<List<CommentDto>>(JsonOptions);
		comments.Should().NotBeNull();
		comments.Should().HaveCount(seededComments.Count - 1);
		comments!.Should().NotContain(c => c.Title == commentToArchive.Title);
	}

	[Fact]
	public async Task GetComments_IncludesArchivedWhenRequested()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var seededComments = await SeedCommentsAsync(issue, 3);

		// Archive one comment
		await using var context = CreateDbContext();
		var commentToArchive = context.Comments.First();
		commentToArchive.Archived = true;
		await context.SaveChangesAsync();

		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/issues/{issue.Id}/comments?includeArchived=true");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var comments = await response.Content.ReadFromJsonAsync<List<CommentDto>>(JsonOptions);
		comments.Should().NotBeNull();
		comments.Should().HaveCount(seededComments.Count);
	}

	#endregion

	#region POST /api/issues/{issueId}/comments - Add Comment to Issue

	[Fact]
	public async Task AddComment_ReturnsCreated_WhenValidRequest()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();
		var request = new AddCommentRequest("New Comment", "This is a new test comment description");

		// Act
		var response = await client.PostAsJsonAsync($"/api/issues/{issue.Id}/comments", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var comment = await response.Content.ReadFromJsonAsync<CommentDto>(JsonOptions);
		comment.Should().NotBeNull();
		comment!.Title.Should().Be(request.Title);
		comment.Description.Should().Be(request.Description);
		comment.IssueId.Should().Be(issue.Id);
		comment.Archived.Should().BeFalse();
		response.Headers.Location.Should().NotBeNull();
	}

	[Fact]
	public async Task AddComment_ReturnsBadRequest_WhenTitleIsEmpty()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();
		var request = new AddCommentRequest("", "Valid description");

		// Act
		var response = await client.PostAsJsonAsync($"/api/issues/{issue.Id}/comments", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task AddComment_ReturnsBadRequest_WhenDescriptionIsEmpty()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();
		var request = new AddCommentRequest("Valid Title", "");

		// Act
		var response = await client.PostAsJsonAsync($"/api/issues/{issue.Id}/comments", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task AddComment_ReturnsNotFound_WhenIssueDoesNotExist()
	{
		// Arrange
		var nonExistentIssueId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();
		var request = new AddCommentRequest("New Comment", "Comment description");

		// Act
		var response = await client.PostAsJsonAsync($"/api/issues/{nonExistentIssueId}/comments", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task AddComment_SetsAuthorFromAuthenticatedUser()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAuthenticatedClient();
		var request = new AddCommentRequest("New Comment", "Comment with author check");

		// Act
		var response = await client.PostAsJsonAsync($"/api/issues/{issue.Id}/comments", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var comment = await response.Content.ReadFromJsonAsync<CommentDto>(JsonOptions);
		comment.Should().NotBeNull();
		comment!.Author.Should().NotBeNull();
		comment.Author.Id.Should().Be(TestAuthHandler.TestUserId);
		comment.Author.Name.Should().Be(TestAuthHandler.TestUserName);
	}

	#endregion

	#region PUT /api/comments/{id} - Update Comment

	[Fact]
	public async Task UpdateComment_ReturnsOk_WhenValidRequest()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue);
		using var client = CreateAuthenticatedClient();
		var request = new UpdateCommentRequest("Updated Title", "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/comments/{comment.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var updatedComment = await response.Content.ReadFromJsonAsync<CommentDto>(JsonOptions);
		updatedComment.Should().NotBeNull();
		updatedComment!.Title.Should().Be(request.Title);
		updatedComment.Description.Should().Be(request.Description);
		updatedComment.DateModified.Should().NotBeNull();
	}

	[Fact]
	public async Task UpdateComment_ReturnsNotFound_WhenCommentDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();
		var request = new UpdateCommentRequest("Updated Title", "Updated description");

		// Act
		var response = await client.PutAsJsonAsync($"/api/comments/{nonExistentId}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateComment_ReturnsBadRequest_WhenTitleIsEmpty()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue);
		using var client = CreateAuthenticatedClient();
		var request = new UpdateCommentRequest("", "Valid description");

		// Act
		var response = await client.PutAsJsonAsync($"/api/comments/{comment.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UpdateComment_ReturnsBadRequest_WhenDescriptionIsEmpty()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue);
		using var client = CreateAuthenticatedClient();
		var request = new UpdateCommentRequest("Valid Title", "");

		// Act
		var response = await client.PutAsJsonAsync($"/api/comments/{comment.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UpdateComment_ReturnsForbidden_WhenNotOwner()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue, authorId: "different-user-id");
		using var client = CreateAuthenticatedClient();
		var request = new UpdateCommentRequest("Updated Title", "Updated description");

		// Act
		var response = await client.PutAsJsonAsync($"/api/comments/{comment.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	#endregion

	#region DELETE /api/comments/{id} - Delete Comment

	[Fact]
	public async Task DeleteComment_ReturnsNoContent_WhenOwnerDeletes()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/comments/{comment.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteComment_ReturnsNoContent_WhenAdminDeletes()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue, authorId: "different-user-id");
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.DeleteAsync($"/api/comments/{comment.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteComment_ReturnsNotFound_WhenCommentDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/comments/{nonExistentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteComment_ReturnsForbidden_WhenNotOwnerOrAdmin()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue, authorId: "different-user-id");
		using var client = CreateAuthenticatedClient("User");

		// Act
		var response = await client.DeleteAsync($"/api/comments/{comment.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task DeleteComment_ArchivesInsteadOfHardDelete()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue);
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/comments/{comment.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NoContent);

		// Verify comment is archived, not deleted
		await using var context = CreateDbContext();
		var archivedComment = context.Comments.FirstOrDefault(c => c.Id == comment.Id);
		archivedComment.Should().NotBeNull();
		archivedComment!.Archived.Should().BeTrue();
	}

	#endregion

	#region Anonymous Access Tests

	[Fact]
	public async Task GetComments_DeniesAnonymousAccess()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync($"/api/issues/{issue.Id}/comments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task AddComment_DeniesAnonymousAccess()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		using var client = CreateAnonymousClient();
		var request = new AddCommentRequest("New Comment", "Comment description");

		// Act
		var response = await client.PostAsJsonAsync($"/api/issues/{issue.Id}/comments", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task UpdateComment_DeniesAnonymousAccess()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue);
		using var client = CreateAnonymousClient();
		var request = new UpdateCommentRequest("Updated Title", "Updated description");

		// Act
		var response = await client.PutAsJsonAsync($"/api/comments/{comment.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task DeleteComment_DeniesAnonymousAccess()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var comment = await SeedCommentAsync(issue);
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.DeleteAsync($"/api/comments/{comment.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion
}
