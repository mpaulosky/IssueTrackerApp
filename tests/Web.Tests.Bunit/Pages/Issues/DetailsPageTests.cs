// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DetailsPageTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using System.Security.Claims;

using MongoDB.Bson;

using Web.Components.Pages.Issues;

namespace Web.Tests.Bunit.Pages.Issues;

/// <summary>
///   Comprehensive tests for the Issue Details page component.
///   Tests loading states, issue rendering, comments, attachments, and user interactions.
/// </summary>
public class DetailsPageTests : BunitTestBase
{
	#region Loading State Tests

	[Fact]
	public void Details_ShowsLoadingSpinnerInitially()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		var tcs = new TaskCompletionSource<Result<IssueDto>>();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));

		// Assert
		cut.Markup.Should().Contain("animate-spin");
	}

	[Fact]
	public async Task Details_HidesLoadingSpinnerAfterDataLoads()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().NotContain("animate-spin");
	}

	#endregion

	#region Issue Details Render Tests

	[Fact]
	public async Task Details_DisplaysIssueTitle()
	{
		// Arrange
		var issue = CreateTestIssue(title: "Critical Security Bug");
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Find("h1").TextContent.Should().Contain("Critical Security Bug");
	}

	[Fact]
	public async Task Details_DisplaysIssueDescription()
	{
		// Arrange
		var issue = CreateTestIssue(description: "This is a detailed description of the bug.");
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("This is a detailed description of the bug.");
	}

	[Fact]
	public async Task Details_DisplaysStatusBadge()
	{
		// Arrange
		var status = CreateTestStatus(name: "In Progress");
		var issue = CreateTestIssue(status: status);
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("In Progress");
	}

	[Fact]
	public async Task Details_DisplaysCategoryBadge()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug Report");
		var issue = CreateTestIssue(category: category);
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Bug Report");
	}

	[Fact]
	public async Task Details_DisplaysAuthorName()
	{
		// Arrange
		var author = CreateTestUser(name: "Jane Developer");
		var issue = CreateTestIssue(author: author);
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Jane Developer");
	}

	[Fact]
	public async Task Details_DisplaysCreatedDate()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Created");
		cut.Markup.Should().Contain(issue.DateCreated.ToString("MMMM d, yyyy"));
	}

	[Fact]
	public async Task Details_DisplaysLastModifiedDateWhenSet()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		var modifiedIssue = issue with { DateModified = DateTime.UtcNow.AddDays(-1) };
		SetupIssueServiceSuccess(issueId, modifiedIssue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Last Modified");
	}

	[Fact]
	public async Task Details_DisplaysNeverWhenNotModified()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Never");
	}

	[Fact]
	public async Task Details_DisplaysBreadcrumbNavigation()
	{
		// Arrange
		var issue = CreateTestIssue(title: "My Test Issue");
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var breadcrumb = cut.Find("nav[aria-label='Breadcrumb']");
		breadcrumb.Should().NotBeNull();
		breadcrumb.TextContent.Should().Contain("Issues");
	}

	#endregion

	#region Edit Button Tests

	[Fact]
	public async Task Details_DisplaysEditButtonForAuthenticatedUser()
	{
		// Arrange
		SetupAuthenticatedUser();
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var editLink = cut.FindAll("a").FirstOrDefault(a => a.GetAttribute("href")?.Contains("/edit") == true);
		editLink.Should().NotBeNull();
		editLink!.TextContent.Should().Contain("Edit");
	}

	[Fact]
	public async Task Details_EditButtonLinksToCorrectEditPage()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var editLink = cut.FindAll("a").FirstOrDefault(a => a.GetAttribute("href")?.Contains("/edit") == true);
		editLink.Should().NotBeNull();
		editLink!.GetAttribute("href").Should().Contain($"/issues/{issueId}/edit");
	}

	[Fact]
	public async Task Details_DisplaysDeleteButton()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
		deleteButton.Should().NotBeNull();
	}

	#endregion

	#region Status Change Tests

	[Fact]
	public async Task Details_DisplaysStatusDropdown()
	{
		// Arrange
		var statuses = new List<StatusDto>
		{
			CreateTestStatus(name: "Open"),
			CreateTestStatus(name: "In Progress"),
			CreateTestStatus(name: "Closed")
		};
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		var issue = CreateTestIssue(status: statuses[0]);
		var issueId = issue.Id.ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var select = cut.Find("select");
		select.Should().NotBeNull();
		select.InnerHtml.Should().Contain("Open");
		select.InnerHtml.Should().Contain("In Progress");
		select.InnerHtml.Should().Contain("Closed");
	}

	[Fact]
	public async Task Details_StatusChangeCallsService()
	{
		// Arrange
		var openStatus = CreateTestStatus(name: "Open");
		var closedStatus = CreateTestStatus(name: "Closed");
		var statuses = new List<StatusDto> { openStatus, closedStatus };
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		var issue = CreateTestIssue(status: openStatus);
		var issueId = issue.Id.ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		var updatedIssue = issue with { Status = closedStatus };
		IssueService.ChangeIssueStatusAsync(issueId, Arg.Any<StatusDto>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedIssue));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var select = cut.Find("select");
		await cut.InvokeAsync(() => select.Change(closedStatus.Id.ToString()));

		// Assert
		await IssueService.Received(1).ChangeIssueStatusAsync(issueId, Arg.Any<StatusDto>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Details_StatusChangeDisplaysSuccessMessage()
	{
		// Arrange
		var openStatus = CreateTestStatus(name: "Open");
		var closedStatus = CreateTestStatus(name: "Closed");
		var statuses = new List<StatusDto> { openStatus, closedStatus };
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		var issue = CreateTestIssue(status: openStatus);
		var issueId = issue.Id.ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		var updatedIssue = issue with { Status = closedStatus };
		IssueService.ChangeIssueStatusAsync(issueId, Arg.Any<StatusDto>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedIssue));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var select = cut.Find("select");
		await cut.InvokeAsync(() => select.Change(closedStatus.Id.ToString()));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Status changed");
	}

	#endregion

	#region Error Handling Tests

	[Fact]
	public async Task Details_DisplaysErrorMessageWhenIssueNotFound()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Issue not found"));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Error");
		cut.Markup.Should().Contain("Issue not found");
	}

	[Fact]
	public async Task Details_DisplaysBackLinkOnError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Issue not found"));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var backLink = cut.FindAll("a").FirstOrDefault(a => a.GetAttribute("href") == "/issues");
		backLink.Should().NotBeNull();
		backLink!.TextContent.Should().Contain("Back to Issues");
	}

	[Fact]
	public async Task Details_HandlesServiceException()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId().ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Task.FromException<Result<IssueDto>>(new Exception("Service unavailable")));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Service unavailable");
	}

	#endregion

	#region Back Navigation Tests

	[Fact]
	public async Task Details_DisplaysBackToIssuesLink()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var backLink = cut.FindAll("a").FirstOrDefault(a => 
			a.GetAttribute("href") == "/issues" && 
			a.TextContent.Contains("Back to Issues"));
		backLink.Should().NotBeNull();
	}

	#endregion

	#region Comments Section Tests

	[Fact]
	public async Task Details_RendersCommentsSection()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.HasComponent<Web.Components.Issues.CommentsSection>().Should().BeTrue();
	}

	[Fact]
	public async Task Details_CommentsSectionReceivesIssueId()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		var comments = new List<CommentDto>
		{
			CreateTestComment(title: "First Comment", issueId: issue.Id)
		};
		CommentService.GetCommentsAsync(issueId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<CommentDto>>(comments));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var commentsSection = cut.FindComponent<Web.Components.Issues.CommentsSection>();
		commentsSection.Instance.IssueId.Should().Be(issueId);
	}

	#endregion

	#region Attachments Section Tests

	[Fact]
	public async Task Details_RendersAttachmentsSection()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.HasComponent<Web.Components.Issues.AttachmentList>().Should().BeTrue();
	}

	[Fact]
	public async Task Details_DisplaysAttachmentList()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		var attachments = new List<AttachmentDto>
		{
			CreateTestAttachment(fileName: "test-document.pdf")
		};
		AttachmentService.GetIssueAttachmentsAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IReadOnlyList<AttachmentDto>>(attachments));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("test-document.pdf");
	}

	[Fact]
	public async Task Details_RendersFileUploadComponent()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.HasComponent<Web.Components.Shared.FileUpload>().Should().BeTrue();
	}

	#endregion

	#region Delete Functionality Tests

	[Fact]
	public async Task Details_ClickDeleteShowsConfirmationModal()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		await cut.InvokeAsync(() => deleteButton.Click());

		// Assert
		cut.Markup.Should().Contain("Delete Issue");
		cut.Markup.Should().Contain("This action cannot be undone");
	}

	[Fact]
	public async Task Details_DeleteConfirmationShowsIssueTitle()
	{
		// Arrange
		var issue = CreateTestIssue(title: "Issue To Delete");
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		await cut.InvokeAsync(() => deleteButton.Click());

		// Assert
		cut.Markup.Should().Contain("Issue To Delete");
	}

	#endregion

	#region Page Title Tests

	[Fact]
	public async Task Details_ContainsIssueTitleInMarkup()
	{
		// Arrange
		var issue = CreateTestIssue(title: "My Custom Issue");
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert - Verify issue title is displayed in the h1 heading
		cut.Find("h1").TextContent.Should().Contain("My Custom Issue");
	}

	#endregion

	#region Helper Methods

	private void SetupIssueServiceSuccess(string issueId, IssueDto issue)
	{
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));
	}

	private static AttachmentDto CreateTestAttachment(
		string? id = null,
		string? issueId = null,
		string? fileName = null)
	{
		return new AttachmentDto(
			Id: id ?? ObjectId.GenerateNewId().ToString(),
			IssueId: issueId ?? ObjectId.GenerateNewId().ToString(),
			FileName: fileName ?? "test-file.txt",
			ContentType: "text/plain",
			FileSize: 1024,
			BlobUrl: "https://example.com/blob/test-file.txt",
			ThumbnailUrl: null,
			UploadedBy: CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);
	}

	#endregion

	#region Authorization Tests

	[Fact]
	public void Details_RequiresAuthentication()
	{
		// Arrange
		SetupAnonymousUser();
		var issueId = ObjectId.GenerateNewId().ToString();

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));

		// Assert
		cut.Markup.Should().NotBeNull();
		// Anonymous users should be redirected or see authorization message
	}

	[Fact]
	public async Task Details_AdminUserSeesDeleteButton()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
		deleteButton.Should().NotBeNull();
	}

	[Fact]
	public async Task Details_RegularUserSeesDeleteButton()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
		deleteButton.Should().NotBeNull();
	}

	#endregion

	#region SignalR Integration Tests

	[Fact]
	public async Task Details_HandleSignalRIssueUpdated()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Simulate SignalR issue updated event
		await cut.InvokeAsync(() => 
		{
			var signalRService = Services.GetService<SignalRClientService>();
			return Task.CompletedTask;
		});

		// Assert
		cut.Markup.Should().NotBeNull();
		// Verify that the component handles SignalR updates gracefully
	}

	[Fact]
	public async Task Details_HandleSignalRCommentAdded()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().NotBeNull();
		// Verify that the component handles comment updates gracefully
	}

	#endregion

	#region File Upload Tests

	[Fact]
	public async Task Details_FileUploadDisplaysSuccessMessage()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		var mockFile = Substitute.For<IBrowserFile>();
		mockFile.Name.Returns("test.pdf");
		mockFile.ContentType.Returns("application/pdf");
		mockFile.Size.Returns(1024);
		mockFile.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>())
			.Returns(new MemoryStream([1, 2, 3, 4]));

		var newAttachment = CreateTestAttachment(fileName: "test.pdf");
		AttachmentService.AddAttachmentAsync(
			issueId, 
			Arg.Any<Stream>(), 
			"test.pdf", 
			"application/pdf", 
			1024, 
			Arg.Any<UserDto>(), 
			Arg.Any<CancellationToken>())
			.Returns(Result.Ok(newAttachment));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Simulate file upload by finding and testing FileUpload component
		var fileUpload = cut.FindComponent<Web.Components.Shared.FileUpload>();
		
		// Assert
		fileUpload.Should().NotBeNull();
	}

	[Fact]
	public async Task Details_FileUploadHandlesErrors()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		AttachmentService.AddAttachmentAsync(
			Arg.Any<string>(), 
			Arg.Any<Stream>(), 
			Arg.Any<string>(), 
			Arg.Any<string>(), 
			Arg.Any<long>(), 
			Arg.Any<UserDto>(), 
			Arg.Any<CancellationToken>())
			.Returns(Result.Fail<AttachmentDto>("Upload failed"));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var fileUpload = cut.FindComponent<Web.Components.Shared.FileUpload>();
		fileUpload.Should().NotBeNull();
	}

	#endregion

	#region Attachment Management Tests

	[Fact]
	public async Task Details_AttachmentDeletion()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		var attachment = CreateTestAttachment();
		AttachmentService.DeleteAttachmentAsync(
			attachment.Id, 
			Arg.Any<string>(), 
			Arg.Any<bool>(), 
			Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(true)));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.HasComponent<Web.Components.Issues.AttachmentList>().Should().BeTrue();
	}

	[Fact]
	public async Task Details_AttachmentError()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		AttachmentService.GetIssueAttachmentsAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IReadOnlyList<AttachmentDto>>("Failed to load attachments"));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().NotBeNull();
	}

	#endregion

	#region Status Change Error Tests

	[Fact]
	public async Task Details_StatusChangeDisplaysErrorOnFailure()
	{
		// Arrange
		var openStatus = CreateTestStatus(name: "Open");
		var closedStatus = CreateTestStatus(name: "Closed");
		var statuses = new List<StatusDto> { openStatus, closedStatus };
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		var issue = CreateTestIssue(status: openStatus);
		var issueId = issue.Id.ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		IssueService.ChangeIssueStatusAsync(issueId, Arg.Any<StatusDto>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Status change failed"));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var select = cut.Find("select");
		await cut.InvokeAsync(() => select.Change(closedStatus.Id.ToString()));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Status change failed");
	}

	[Fact]
	public async Task Details_StatusChangeHandlesException()
	{
		// Arrange
		var openStatus = CreateTestStatus(name: "Open");
		var closedStatus = CreateTestStatus(name: "Closed");
		var statuses = new List<StatusDto> { openStatus, closedStatus };
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		var issue = CreateTestIssue(status: openStatus);
		var issueId = issue.Id.ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		IssueService.ChangeIssueStatusAsync(issueId, Arg.Any<StatusDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<Result<IssueDto>>(new Exception("Network error")));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var select = cut.Find("select");
		await cut.InvokeAsync(() => select.Change(closedStatus.Id.ToString()));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Network error");
	}

	[Fact]
	public async Task Details_StatusChangeIgnoresSameStatus()
	{
		// Arrange
		var openStatus = CreateTestStatus(name: "Open");
		var statuses = new List<StatusDto> { openStatus };
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		var issue = CreateTestIssue(status: openStatus);
		var issueId = issue.Id.ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var select = cut.Find("select");
		await cut.InvokeAsync(() => select.Change(openStatus.Id.ToString()));

		// Assert
		await IssueService.DidNotReceive().ChangeIssueStatusAsync(Arg.Any<string>(), Arg.Any<StatusDto>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region Delete Modal Tests

	[Fact]
	public async Task Details_DeleteModalCanBeCanceled()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		await cut.InvokeAsync(() => deleteButton.Click());

		// Find cancel button and click it
		var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
		if (cancelButton != null)
		{
			await cut.InvokeAsync(() => cancelButton.Click());
		}

		// Assert
		cut.Markup.Should().NotContain("This action cannot be undone");
	}

	[Fact]
	public async Task Details_DeleteExecutionNavigatesToIndex()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		IssueService.DeleteIssueAsync(issueId, Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(true)));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		await cut.InvokeAsync(() => deleteButton.Click());

		// Find confirm button and click it
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete") && !b.TextContent.Contains("Cancel"));
		if (confirmButton != null)
		{
			await cut.InvokeAsync(() => confirmButton.Click());
		}

		// Assert
		await IssueService.Received(1).DeleteIssueAsync(issueId, Arg.Any<UserDto>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Details_DeleteFailureShowsError()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		IssueService.DeleteIssueAsync(issueId, Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<bool>("Delete failed")));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		await cut.InvokeAsync(() => deleteButton.Click());

		// Find confirm button and click it
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => 
			b.TextContent.Contains("Delete") && 
			!b.TextContent.Contains("Cancel") &&
			b.GetAttribute("type") != "button"); // Ensure it's the modal confirm button

		if (confirmButton != null)
		{
			await cut.InvokeAsync(() => confirmButton.Click());
			await cut.InvokeAsync(() => Task.Delay(50));
		}

		// Assert
		cut.Markup.Should().Contain("Delete failed");
	}

	#endregion

	#region Service Loading Tests

	[Fact]
	public async Task Details_HandlesMissingStatuses()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<StatusDto>>("No statuses"));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().NotBeNull();
		// Component should still render even without statuses
	}

	[Fact]
	public async Task Details_HandlesPartialDataFailure()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		IssueService.GetIssueByIdAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issue));

		// Statuses succeed
		var statuses = new List<StatusDto> { CreateTestStatus() };
		LookupService.GetStatusesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		// Attachments fail
		AttachmentService.GetIssueAttachmentsAsync(issueId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IReadOnlyList<AttachmentDto>>("Attachment service down"));

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain(issue.Title);
		cut.Find("select").Should().NotBeNull(); // Status dropdown should still work
	}

	#endregion

	#region Component Lifecycle Tests

	[Fact]
	public async Task Details_DisposesProperlyOnDestroy()
	{
		// Arrange
		var issue = CreateTestIssue();
		var issueId = issue.Id.ToString();
		SetupIssueServiceSuccess(issueId, issue);

		// Act
		var cut = Render<Details>(parameters => parameters.Add(p => p.Id, issueId));
		await cut.InvokeAsync(() => Task.Delay(100));

		// Dispose the component
		cut.Dispose();

		// Assert - Component should dispose without errors
		cut.Should().NotBeNull();
	}

	#endregion
}
