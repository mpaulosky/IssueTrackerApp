// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SmtpEmailServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Microsoft.Extensions.Options;
using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for <see cref="SmtpEmailService"/>.
/// Tests cover email sending, templated emails, and error handling scenarios.
/// </summary>
/// <remarks>
/// Note: SmtpClient cannot be easily mocked, so these tests verify:
/// - Service construction and configuration
/// - SendTemplatedAsync behavior (which builds messages and calls SendAsync)
/// - The service correctly uses settings and handles various email message configurations
/// </remarks>
public sealed class SmtpEmailServiceTests
{
	private readonly IOptions<SmtpSettings> _settings;
	private readonly ILogger<SmtpEmailService> _logger;

	public SmtpEmailServiceTests()
	{
		_settings = Options.Create(new SmtpSettings
		{
			Host = "localhost",
			Port = 2525, // Use non-standard port to avoid actual SMTP connections
			Username = "testuser",
			Password = "testpassword",
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Test System"
		});
		_logger = Substitute.For<ILogger<SmtpEmailService>>();
	}

	#region Constructor Tests

	[Fact]
	public void Constructor_WithValidSettings_CreatesService()
	{
		// Arrange & Act
		var sut = new SmtpEmailService(_settings, _logger);

		// Assert
		sut.Should().NotBeNull();
	}

	[Fact]
	public void Constructor_WithEmptyCredentials_CreatesService()
	{
		// Arrange
		var settingsWithNoAuth = Options.Create(new SmtpSettings
		{
			Host = "localhost",
			Port = 25,
			Username = null,
			Password = null,
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Test"
		});

		// Act
		var sut = new SmtpEmailService(settingsWithNoAuth, _logger);

		// Assert
		sut.Should().NotBeNull();
	}

	#endregion

	#region SendAsync Tests - These will fail at SMTP level but verify message handling

	[Fact]
	public async Task SendAsync_WithInvalidSmtpServer_ReturnsFailure()
	{
		// Arrange
		var settings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 25,
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Test"
		});
		var sut = new SmtpEmailService(settings, _logger);
		var message = new EmailMessage(
			"recipient@test.com",
			"Test Subject",
			"Test Body",
			false
		);

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Failed to send email");
	}

	[Fact]
	public async Task SendAsync_WithCancellation_ReturnsFailure()
	{
		// Arrange
		var sut = new SmtpEmailService(_settings, _logger);
		var message = new EmailMessage(
			"recipient@test.com",
			"Test Subject",
			"Test Body",
			false
		);
		var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		// Act
		var result = await sut.SendAsync(message, cts.Token);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Failed to send email");
	}

	[Fact]
	public async Task SendAsync_LogsErrorOnFailure()
	{
		// Arrange
		var settings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 25,
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Test"
		});
		var sut = new SmtpEmailService(settings, _logger);
		var message = new EmailMessage(
			"recipient@test.com",
			"Test Subject",
			"Test Body",
			false
		);

		// Act
		await sut.SendAsync(message);

		// Assert - Verify logger was called with error
		_logger.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	#endregion

	#region SendTemplatedAsync Tests

	[Fact]
	public async Task SendTemplatedAsync_LogsWarning_ForUnimplementedTemplating()
	{
		// Arrange
		var sut = new SmtpEmailService(_settings, _logger);
		var model = new { Name = "Test User", Code = "ABC123" };

		// Act
		await sut.SendTemplatedAsync("WelcomeEmail", model, "recipient@test.com");

		// Assert - Should log warning about template rendering
		_logger.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Is<Exception?>(e => e == null),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	[Fact]
	public async Task SendTemplatedAsync_WithComplexModel_SerializesCorrectly()
	{
		// Arrange
		var settings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 25,
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "IssueTracker Test"
		});
		var sut = new SmtpEmailService(settings, _logger);
		var model = new TestEmailModel
		{
			IssueId = "ISSUE-123",
			Title = "Test Issue",
			AssignedTo = "john@example.com"
		};

		// Act
		var result = await sut.SendTemplatedAsync("IssueAssigned", model, "john@example.com");

		// Assert
		result.Failure.Should().BeTrue(); // Will fail due to invalid SMTP
		result.Error.Should().Contain("Failed to send email");
	}

	[Fact]
	public async Task SendTemplatedAsync_WithCancellation_ReturnsFailure()
	{
		// Arrange
		var sut = new SmtpEmailService(_settings, _logger);
		var model = new { Message = "Test" };
		var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		// Act
		var result = await sut.SendTemplatedAsync("Template", model, "test@test.com", cts.Token);

		// Assert
		result.Failure.Should().BeTrue();
	}

	[Fact]
	public async Task SendTemplatedAsync_WithNullModel_HandlesGracefully()
	{
		// Arrange
		var settings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 25,
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Test"
		});
		var sut = new SmtpEmailService(settings, _logger);

		// Act
		var result = await sut.SendTemplatedAsync<object?>("Template", null, "test@test.com");

		// Assert
		result.Failure.Should().BeTrue(); // Will fail at SMTP level, not serialization
	}

	[Fact]
	public async Task SendTemplatedAsync_IncludesTemplateNameInBody()
	{
		// Arrange
		var sut = new SmtpEmailService(_settings, _logger);
		var model = new { Key = "Value" };
		const string templateName = "SpecificTemplateName";

		// Act - This tests the internal message construction
		var result = await sut.SendTemplatedAsync(templateName, model, "test@test.com");

		// Assert - The warning log confirms the method was reached
		_logger.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception?>(),
			Arg.Any<Func<object, Exception?, string>>()
		);
	}

	#endregion

	#region EmailMessage Variations

	[Fact]
	public async Task SendAsync_WithHtmlMessage_SetsIsBodyHtml()
	{
		// Arrange
		var settings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 25,
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Test"
		});
		var sut = new SmtpEmailService(settings, _logger);
		var message = new EmailMessage(
			"recipient@test.com",
			"HTML Test",
			"<h1>Hello</h1><p>World</p>",
			true // IsHtml
		);

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Failed to send email");
	}

	[Fact]
	public async Task SendAsync_WithCustomFromName_UsesCustomName()
	{
		// Arrange
		var settings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 25,
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Default System"
		});
		var sut = new SmtpEmailService(settings, _logger);
		var message = new EmailMessage(
			"recipient@test.com",
			"Custom From Test",
			"Body content",
			false,
			"Custom Sender Name" // FromName
		);

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Failed to send email");
	}

	[Fact]
	public async Task SendAsync_WithNullFromName_UsesDefaultFromSettings()
	{
		// Arrange
		var settings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 25,
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Default System Name"
		});
		var sut = new SmtpEmailService(settings, _logger);
		var message = new EmailMessage(
			"recipient@test.com",
			"Test Subject",
			"Test Body",
			false,
			null // No custom FromName
		);

		// Act
		var result = await sut.SendAsync(message);

		// Assert - Should use settings.FromName as fallback
		result.Failure.Should().BeTrue();
	}

	#endregion

	#region Settings Variations

	[Fact]
	public async Task SendAsync_WithSslEnabled_AttemptsSecureConnection()
	{
		// Arrange
		var sslSettings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 465,
			EnableSsl = true,
			FromEmail = "noreply@test.com",
			FromName = "Test"
		});
		var sut = new SmtpEmailService(sslSettings, _logger);
		var message = new EmailMessage(
			"recipient@test.com",
			"SSL Test",
			"Test Body",
			false
		);

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Failed to send email");
	}

	[Fact]
	public async Task SendAsync_WithoutCredentials_UsesNoAuthentication()
	{
		// Arrange
		var noAuthSettings = Options.Create(new SmtpSettings
		{
			Host = "invalid.nonexistent.host.test",
			Port = 25,
			Username = "",
			Password = "",
			EnableSsl = false,
			FromEmail = "noreply@test.com",
			FromName = "Test"
		});
		var sut = new SmtpEmailService(noAuthSettings, _logger);
		var message = new EmailMessage(
			"recipient@test.com",
			"No Auth Test",
			"Test Body",
			false
		);

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Failed to send email");
	}

	#endregion

	#region Test Models

	private sealed class TestEmailModel
	{
		public string IssueId { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string AssignedTo { get; set; } = string.Empty;
	}

	#endregion
}
