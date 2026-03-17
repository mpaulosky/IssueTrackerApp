// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     FileUploadTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using Domain.Models;

using Web.Components.Shared;

namespace Web.Tests.Bunit.Components.Shared;

/// <summary>
///   Tests for FileUpload component.
/// </summary>
public class FileUploadTests : BunitTestBase
{
	#region Render Tests

	[Fact]
	public void FileUpload_Renders_DropZone()
	{
		// Arrange & Act
		var cut = Render<FileUpload>();

		// Assert
		cut.Markup.Should().Contain("Attachments");
		cut.Markup.Should().Contain("Upload a file");
		cut.Markup.Should().Contain("drag and drop");
	}

	[Fact]
	public void FileUpload_Renders_SupportedFileTypes()
	{
		// Arrange & Act
		var cut = Render<FileUpload>();

		// Assert
		cut.Markup.Should().Contain("JPG, PNG, GIF, WEBP, PDF, TXT, MD up to 10MB");
	}

	[Fact]
	public void FileUpload_Renders_InputFile()
	{
		// Arrange & Act
		var cut = Render<FileUpload>();

		// Assert
		var input = cut.Find("input[type='file']");
		input.Should().NotBeNull();
		input.GetAttribute("accept").Should().Contain(".jpg");
		input.GetAttribute("accept").Should().Contain(".pdf");
		input.GetAttribute("accept").Should().Contain(".md");
	}

	#endregion

	#region File Selection Tests

	[Fact]
	public async Task FileUpload_WhenValidImageSelected_InvokesCallback()
	{
		// Arrange
		IBrowserFile? selectedFile = null;
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, f => selectedFile = f))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test content", "test.png", contentType: "image/png")
		));

		// Assert
		selectedFile.Should().NotBeNull();
		selectedFile!.Name.Should().Be("test.png");
		selectedFile.ContentType.Should().Be("image/png");
	}

	[Fact]
	public async Task FileUpload_WhenValidPdfSelected_InvokesCallback()
	{
		// Arrange
		IBrowserFile? selectedFile = null;
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, f => selectedFile = f))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test content", "document.pdf", contentType: "application/pdf")
		));

		// Assert
		selectedFile.Should().NotBeNull();
		selectedFile!.Name.Should().Be("document.pdf");
		selectedFile.ContentType.Should().Be("application/pdf");
	}

	[Fact]
	public async Task FileUpload_WhenMarkdownSelected_InvokesCallback()
	{
		// Arrange
		IBrowserFile? selectedFile = null;
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, f => selectedFile = f))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("# Readme", "readme.md", contentType: "text/markdown")
		));

		// Assert
		selectedFile.Should().NotBeNull();
		selectedFile!.Name.Should().Be("readme.md");
	}

	#endregion

	#region File Validation Tests

	[Fact]
	public async Task FileUpload_WhenFileTooLarge_ShowsError()
	{
		// Arrange
		string? errorMessage = null;
		IBrowserFile? selectedFile = null;

		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, f => selectedFile = f))
			.Add(p => p.OnUploadError, EventCallback.Factory.Create<string>(this, e => errorMessage = e))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Create a file larger than 10MB
		var largeContent = new string('x', (int)(FileValidationConstants.MAX_FILE_SIZE + 1));

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText(largeContent, "large.png", contentType: "image/png")
		));

		// Assert
		selectedFile.Should().BeNull();
		errorMessage.Should().Contain("10MB");
		cut.Markup.Should().Contain("10MB");
	}

	[Fact]
	public async Task FileUpload_WhenInvalidFileType_ShowsError()
	{
		// Arrange
		string? errorMessage = null;
		IBrowserFile? selectedFile = null;

		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, f => selectedFile = f))
			.Add(p => p.OnUploadError, EventCallback.Factory.Create<string>(this, e => errorMessage = e))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test", "script.exe", contentType: "application/x-msdownload")
		));

		// Assert
		selectedFile.Should().BeNull();
		errorMessage.Should().Contain("not allowed");
		cut.Markup.Should().Contain("not allowed");
	}

	[Fact]
	public async Task FileUpload_WhenUnsupportedContentType_ShowsError()
	{
		// Arrange
		string? errorMessage = null;

		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnUploadError, EventCallback.Factory.Create<string>(this, e => errorMessage = e))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test", "data.xml", contentType: "application/xml")
		));

		// Assert
		errorMessage.Should().Contain("not allowed");
	}

	#endregion

	#region Drag and Drop Tests

	[Fact]
	public void FileUpload_OnDragEnter_HighlightsDropZone()
	{
		// Arrange
		var cut = Render<FileUpload>();
		var dropZone = cut.Find("div.border-2");

		// Act
		dropZone.DragEnter();

		// Assert
		dropZone.ClassList.Should().Contain("border-primary-500");
	}

	[Fact]
	public void FileUpload_OnDragLeave_RemovesHighlight()
	{
		// Arrange
		var cut = Render<FileUpload>();
		var dropZone = cut.Find("div.border-2");

		// Act
		dropZone.DragEnter();
		dropZone.DragLeave();

		// Assert
		dropZone.ClassList.Should().NotContain("border-primary-500");
	}

	[Fact]
	public void FileUpload_OnDrop_RemovesHighlight()
	{
		// Arrange
		var cut = Render<FileUpload>();
		var dropZone = cut.Find("div.border-2");

		// Act
		dropZone.DragEnter();
		dropZone.Drop();

		// Assert
		dropZone.ClassList.Should().NotContain("border-primary-500");
	}

	#endregion

	#region Upload Progress Tests

	[Fact]
	public async Task FileUpload_DuringUpload_ShowsProgressIndicator()
	{
		// Arrange
		var tcs = new TaskCompletionSource<bool>();
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, async _ =>
			{
				// Delay to simulate upload
				await tcs.Task;
			}))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act - Start upload (don't await)
		var uploadTask = cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test", "test.png", contentType: "image/png")
		));

		// Assert - Progress should show during upload
		cut.Markup.Should().Contain("Uploading");
		cut.Markup.Should().Contain("test.png");

		// Complete the upload
		tcs.SetResult(true);
		await uploadTask;
	}

	[Fact]
	public async Task FileUpload_AfterUploadCompletes_HidesProgressIndicator()
	{
		// Arrange
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, _ => Task.CompletedTask))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test", "test.png", contentType: "image/png")
		));

		// Assert
		cut.Markup.Should().NotContain("Uploading");
		cut.Markup.Should().Contain("Upload a file");
	}

	#endregion

	#region Reset Tests

	[Fact]
	public async Task FileUpload_Reset_ClearsErrorMessage()
	{
		// Arrange
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnUploadError, EventCallback.Factory.Create<string>(this, _ => { }))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Trigger an error first
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test", "script.exe", contentType: "application/x-msdownload")
		));

		// Verify error is shown
		cut.Markup.Should().Contain("not allowed");

		// Act - Call Reset inside the component's context
		await cut.InvokeAsync(() => cut.Instance.Reset());

		// Assert
		cut.Markup.Should().NotContain("not allowed");
	}

	#endregion

	#region Accept String Tests

	[Fact]
	public void FileUpload_InputFileAccept_IncludesAllSupportedTypes()
	{
		// Arrange & Act
		var cut = Render<FileUpload>();

		// Assert
		var input = cut.Find("input[type='file']");
		var accept = input.GetAttribute("accept");

		// Image types
		accept.Should().Contain(".jpg");
		accept.Should().Contain(".jpeg");
		accept.Should().Contain(".png");
		accept.Should().Contain(".gif");
		accept.Should().Contain(".webp");

		// Document types
		accept.Should().Contain(".pdf");
		accept.Should().Contain(".txt");
		accept.Should().Contain(".md");
	}

	#endregion

	#region Error Display Tests

	[Fact]
	public async Task FileUpload_WhenError_ShowsErrorIcon()
	{
		// Arrange
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnUploadError, EventCallback.Factory.Create<string>(this, _ => { }))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act - Trigger an error
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test", "script.exe", contentType: "application/x-msdownload")
		));

		// Assert - Error div should contain an SVG icon
		var errorDiv = cut.Find("div.bg-red-50");
		errorDiv.Should().NotBeNull();
		errorDiv.InnerHtml.Should().Contain("svg");
	}

	[Fact]
	public async Task FileUpload_WhenNoError_HidesErrorDiv()
	{
		// Arrange
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, _ => Task.CompletedTask))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act - Upload valid file
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test", "test.png", contentType: "image/png")
		));

		// Assert
		cut.FindAll("div.bg-red-50").Should().BeEmpty();
	}

	#endregion

	#region Multiple File Type Tests

	[Theory]
	[InlineData("image/jpeg", "photo.jpg")]
	[InlineData("image/png", "screenshot.png")]
	[InlineData("image/gif", "animation.gif")]
	[InlineData("image/webp", "modern.webp")]
	[InlineData("application/pdf", "document.pdf")]
	[InlineData("text/plain", "notes.txt")]
	[InlineData("text/markdown", "readme.md")]
	public async Task FileUpload_AcceptsAllValidFileTypes(string contentType, string fileName)
	{
		// Arrange
		IBrowserFile? selectedFile = null;
		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, f => selectedFile = f))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test content", fileName, contentType: contentType)
		));

		// Assert
		selectedFile.Should().NotBeNull();
		selectedFile!.Name.Should().Be(fileName);
	}

	[Theory]
	[InlineData("application/x-msdownload", "virus.exe")]
	[InlineData("application/javascript", "script.js")]
	[InlineData("application/x-sh", "script.sh")]
	[InlineData("text/html", "page.html")]
	public async Task FileUpload_RejectsInvalidFileTypes(string contentType, string fileName)
	{
		// Arrange
		IBrowserFile? selectedFile = null;
		string? errorMessage = null;

		var cut = Render<FileUpload>(parameters => parameters
			.Add(p => p.OnFileSelected, EventCallback.Factory.Create<IBrowserFile>(this, f => selectedFile = f))
			.Add(p => p.OnUploadError, EventCallback.Factory.Create<string>(this, e => errorMessage = e))
		);

		var inputFile = cut.FindComponent<InputFile>();

		// Act
		await cut.InvokeAsync(() => inputFile.UploadFiles(
			InputFileContent.CreateFromText("test content", fileName, contentType: contentType)
		));

		// Assert
		selectedFile.Should().BeNull();
		errorMessage.Should().NotBeNullOrEmpty();
	}

	#endregion
}
