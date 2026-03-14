// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AttachmentEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using System.Text;
using Domain.DTOs;
using Domain.Models;
using MongoDB.Bson;
using Persistence.MongoDb;

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests for Attachment API endpoints.
/// </summary>
public class AttachmentEndpointTests : IntegrationTestBase
{
	public AttachmentEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region GET /api/issues/{issueId}/attachments Tests

	[Fact]
	public async Task GetIssueAttachments_WithValidIssueId_ReturnsOkWithAttachments()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);
		var attachment = await SeedAttachmentAsync(issue.Id.ToString());

		// Act
		var response = await client.GetAsync($"/api/issues/{issue.Id}/attachments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var attachments = await response.Content.ReadFromJsonAsync<List<AttachmentDto>>();
		attachments.Should().NotBeNull();
		attachments.Should().HaveCount(1);
		attachments![0].FileName.Should().Be(attachment.FileName);
	}

	[Fact]
	public async Task GetIssueAttachments_WithNoAttachments_ReturnsEmptyList()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// Act
		var response = await client.GetAsync($"/api/issues/{issue.Id}/attachments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var attachments = await response.Content.ReadFromJsonAsync<List<AttachmentDto>>();
		attachments.Should().NotBeNull();
		attachments.Should().BeEmpty();
	}

	[Fact]
	public async Task GetIssueAttachments_WithAnonymousUser_ReturnsUnauthorized()
	{
		// Arrange
		var client = CreateAnonymousClient();
		var issueId = ObjectId.GenerateNewId().ToString();

		// Act
		var response = await client.GetAsync($"/api/issues/{issueId}/attachments");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion

	#region POST /api/issues/{issueId}/attachments Tests

	[Fact]
	public async Task UploadAttachment_WithValidFile_ReturnsCreated()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		using var content = CreateMultipartContent("test-file.txt", "text/plain", "Hello, World!");

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/attachments", content);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var attachment = await response.Content.ReadFromJsonAsync<AttachmentDto>();
		attachment.Should().NotBeNull();
		attachment!.FileName.Should().Be("test-file.txt");
		attachment.ContentType.Should().Be("text/plain");
		attachment.IssueId.Should().Be(issue.Id.ToString());
	}

	[Fact]
	public async Task UploadAttachment_WithImageFile_ReturnsCreated()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// Create a minimal valid PNG file (1x1 transparent pixel)
		var pngBytes = Convert.FromBase64String(
			"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
		using var content = CreateMultipartContent("test-image.png", "image/png", pngBytes);

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/attachments", content);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var attachment = await response.Content.ReadFromJsonAsync<AttachmentDto>();
		attachment.Should().NotBeNull();
		attachment!.FileName.Should().Be("test-image.png");
		attachment.ContentType.Should().Be("image/png");
		attachment.IsImage.Should().BeTrue();
	}

	[Fact]
	public async Task UploadAttachment_WithPdfFile_ReturnsCreated()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// Create a minimal valid PDF file
		var pdfContent = "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 0/Kids[]>>endobj\nxref\n0 3\ntrailer<</Size 3/Root 1 0 R>>startxref\n99\n%%EOF";
		using var content = CreateMultipartContent("document.pdf", "application/pdf", pdfContent);

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/attachments", content);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var attachment = await response.Content.ReadFromJsonAsync<AttachmentDto>();
		attachment.Should().NotBeNull();
		attachment!.FileName.Should().Be("document.pdf");
		attachment.ContentType.Should().Be("application/pdf");
	}

	[Fact]
	public async Task UploadAttachment_WithInvalidFileType_ReturnsBadRequest()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		using var content = CreateMultipartContent("script.exe", "application/octet-stream", "executable content");

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/attachments", content);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var error = await response.Content.ReadAsStringAsync();
		error.Should().Contain("not allowed");
	}

	[Fact]
	public async Task UploadAttachment_WithFileSizeExceedingLimit_ReturnsBadRequest()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// Create content larger than 10MB limit
		var largeContent = new string('x', (int)(FileValidationConstants.MAX_FILE_SIZE + 1024));
		using var content = CreateMultipartContent("large-file.txt", "text/plain", largeContent);

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/attachments", content);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var error = await response.Content.ReadAsStringAsync();
		error.Should().Contain("size");
	}

	[Fact]
	public async Task UploadAttachment_WithNoFile_ReturnsBadRequest()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		using var content = new MultipartFormDataContent();

		// Act
		var response = await client.PostAsync($"/api/issues/{issue.Id}/attachments", content);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var error = await response.Content.ReadAsStringAsync();
		error.Should().Contain("No file");
	}

	[Fact]
	public async Task UploadAttachment_WithAnonymousUser_ReturnsUnauthorized()
	{
		// Arrange
		var client = CreateAnonymousClient();
		var issueId = ObjectId.GenerateNewId().ToString();

		using var content = CreateMultipartContent("test.txt", "text/plain", "content");

		// Act
		var response = await client.PostAsync($"/api/issues/{issueId}/attachments", content);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion

	#region GET /api/attachments/{id} Tests

	[Fact]
	public async Task DownloadAttachment_WithValidId_ReturnsFileContent()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// First upload a file
		using var uploadContent = CreateMultipartContent("download-test.txt", "text/plain", "Download me!");
		var uploadResponse = await client.PostAsync($"/api/issues/{issue.Id}/attachments", uploadContent);
		uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var uploadedAttachment = await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>();

		// Act
		var response = await client.GetAsync($"/api/attachments/{uploadedAttachment!.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
		var content = await response.Content.ReadAsStringAsync();
		content.Should().Be("Download me!");
	}

	[Fact]
	public async Task DownloadAttachment_WithNonExistentId_ReturnsNotFound()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var nonExistentId = ObjectId.GenerateNewId().ToString();

		// Act
		var response = await client.GetAsync($"/api/attachments/{nonExistentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DownloadAttachment_WithAnonymousUser_ReturnsUnauthorized()
	{
		// Arrange
		var client = CreateAnonymousClient();
		var attachmentId = ObjectId.GenerateNewId().ToString();

		// Act
		var response = await client.GetAsync($"/api/attachments/{attachmentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion

	#region DELETE /api/attachments/{id} Tests

	[Fact]
	public async Task DeleteAttachment_ByOwner_ReturnsNoContent()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// First upload a file
		using var uploadContent = CreateMultipartContent("to-delete.txt", "text/plain", "Delete me!");
		var uploadResponse = await client.PostAsync($"/api/issues/{issue.Id}/attachments", uploadContent);
		uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var uploadedAttachment = await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>();

		// Act
		var response = await client.DeleteAsync($"/api/attachments/{uploadedAttachment!.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NoContent);

		// Verify deletion
		var getResponse = await client.GetAsync($"/api/attachments/{uploadedAttachment.Id}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteAttachment_ByAdmin_ReturnsNoContent()
	{
		// Arrange
		var userClient = CreateAuthenticatedClient("test-user-1", "User");
		var adminClient = CreateAuthenticatedClient("Admin");

		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// Upload a file as regular user
		using var uploadContent = CreateMultipartContent("admin-delete.txt", "text/plain", "Admin will delete me!");
		var uploadResponse = await userClient.PostAsync($"/api/issues/{issue.Id}/attachments", uploadContent);
		uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var uploadedAttachment = await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>();

		// Act - Admin deletes the attachment
		var response = await adminClient.DeleteAsync($"/api/attachments/{uploadedAttachment!.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task DeleteAttachment_ByNonOwnerNonAdmin_ReturnsForbidden()
	{
		// Arrange
		var ownerClient = CreateAuthenticatedClient("owner-user-id", "User");
		var otherClient = CreateAuthenticatedClient("other-user-id", "User");

		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// Upload a file as owner
		using var uploadContent = CreateMultipartContent("forbidden.txt", "text/plain", "Can't touch this!");
		var uploadResponse = await ownerClient.PostAsync($"/api/issues/{issue.Id}/attachments", uploadContent);
		uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var uploadedAttachment = await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>();

		// Act - Another user tries to delete
		var response = await otherClient.DeleteAsync($"/api/attachments/{uploadedAttachment!.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task DeleteAttachment_WithNonExistentId_ReturnsNotFound()
	{
		// Arrange
		var client = CreateAuthenticatedClient();
		var nonExistentId = ObjectId.GenerateNewId().ToString();

		// Act
		var response = await client.DeleteAsync($"/api/attachments/{nonExistentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task DeleteAttachment_WithAnonymousUser_ReturnsUnauthorized()
	{
		// Arrange
		var client = CreateAnonymousClient();
		var attachmentId = ObjectId.GenerateNewId().ToString();

		// Act
		var response = await client.DeleteAsync($"/api/attachments/{attachmentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion

	#region Helper Methods

	/// <summary>
	///   Creates MultipartFormDataContent for file upload with string content.
	/// </summary>
	private static MultipartFormDataContent CreateMultipartContent(
		string fileName,
		string contentType,
		string content)
	{
		var bytes = Encoding.UTF8.GetBytes(content);
		return CreateMultipartContent(fileName, contentType, bytes);
	}

	/// <summary>
	///   Creates MultipartFormDataContent for file upload with byte content.
	/// </summary>
	private static MultipartFormDataContent CreateMultipartContent(
		string fileName,
		string contentType,
		byte[] content)
	{
		var multipartContent = new MultipartFormDataContent();
		var fileContent = new ByteArrayContent(content);
		fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
		multipartContent.Add(fileContent, "file", fileName);
		return multipartContent;
	}

	/// <summary>
	///   Seeds a test attachment into the database.
	/// </summary>
	private async Task<Attachment> SeedAttachmentAsync(
		string issueId,
		string fileName = "test-attachment.txt",
		string contentType = "text/plain")
	{
		await using var context = Factory.CreateDbContext();

		var attachment = new Attachment
		{
			Id = ObjectId.GenerateNewId(),
			IssueId = ObjectId.Parse(issueId),
			FileName = fileName,
			ContentType = contentType,
			FileSize = 1024,
			BlobUrl = $"/uploads/{Guid.NewGuid()}_{fileName}",
			UploadedBy = new UserInfo
			{
				Id = TestAuthHandler.TestUserId,
				Name = TestAuthHandler.TestUserName,
				Email = TestAuthHandler.TestUserEmail
			},
			UploadedAt = DateTime.UtcNow
		};

		context.Attachments.Add(attachment);
		await context.SaveChangesAsync();

		return attachment;
	}

	#endregion
}
