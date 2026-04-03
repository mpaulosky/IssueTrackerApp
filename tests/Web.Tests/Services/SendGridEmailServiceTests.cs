// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SendGridEmailServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Net.Http;
using System.Net.Http.Headers;

using Microsoft.Extensions.Options;

using NSubstitute.ExceptionExtensions;

using SendGrid;
using SendGrid.Helpers.Mail;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for <see cref="SendGridEmailService" />.
///   Covers <see cref="SendGridEmailService.SendAsync" /> and
///   <see cref="SendGridEmailService.SendTemplatedAsync{T}" /> via a mocked
///   <see cref="ISendGridClient" /> injected through the internal testing constructor.
/// </summary>
public sealed class SendGridEmailServiceTests
{
	private readonly IOptions<SendGridSettings> _settings;
	private readonly ILogger<SendGridEmailService> _logger;
	private readonly ISendGridClient _client;

	public SendGridEmailServiceTests()
	{
		_settings = Options.Create(new SendGridSettings
		{
			ApiKey = "SG.test-key",
			FromEmail = "noreply@issuetracker.test",
			FromName = "IssueTracker Test"
		});
		_logger = Substitute.For<ILogger<SendGridEmailService>>();
		_client = Substitute.For<ISendGridClient>();
	}

	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	/// <summary>
	///   Builds a <see cref="Response" /> with the given status code and optional body.
	/// </summary>
	private static Response MakeResponse(HttpStatusCode statusCode, string body = "")
	{
		var msg = new HttpResponseMessage();
		return new Response(statusCode, new StringContent(body), msg.Headers);
	}

	private SendGridEmailService CreateSut() =>
		new SendGridEmailService(_settings, _logger, _client);

	// -----------------------------------------------------------------------
	// Constructor
	// -----------------------------------------------------------------------

	#region Constructor

	[Fact]
	public void Constructor_WithValidSettings_CreatesService()
	{
		// Arrange & Act
		var sut = CreateSut();

		// Assert
		sut.Should().NotBeNull();
	}

	#endregion

	// -----------------------------------------------------------------------
	// SendAsync — success paths
	// -----------------------------------------------------------------------

	#region SendAsync Success

	[Fact]
	public async Task SendAsync_WhenClientReturns2xx_ReturnsOk()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body");

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task SendAsync_WhenSuccessful_LogsInformation()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.OK));

		var sut = CreateSut();
		var message = new EmailMessage("user@example.com", "Hello", "World");

		// Act
		await sut.SendAsync(message);

		// Assert
		_logger.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Is<Exception?>(e => e == null),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task SendAsync_WithHtmlBody_SendsHtmlContent()
	{
		// Arrange
		SendGridMessage? captured = null;
		_client.SendEmailAsync(
				Arg.Do<SendGridMessage>(m => captured = m),
				Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "<h1>Hi</h1>", IsHtml: true);

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Success.Should().BeTrue();
		captured.Should().NotBeNull();
		// MailHelper stores content in the Contents list; type=text/html for HTML messages
		captured!.Contents.Should().ContainSingle(c => c.Type == "text/html" && c.Value == "<h1>Hi</h1>");
	}

	[Fact]
	public async Task SendAsync_WithPlainTextBody_SendsPlainContent()
	{
		// Arrange
		SendGridMessage? captured = null;
		_client.SendEmailAsync(
				Arg.Do<SendGridMessage>(m => captured = m),
				Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Plain text", IsHtml: false);

		// Act
		await sut.SendAsync(message);

		// Assert
		// MailHelper stores content in the Contents list; type=text/plain for plain messages
		captured!.Contents.Should().ContainSingle(c => c.Type == "text/plain" && c.Value == "Plain text");
	}

	[Fact]
	public async Task SendAsync_WithCustomFromName_UsesCustomName()
	{
		// Arrange
		SendGridMessage? captured = null;
		_client.SendEmailAsync(
				Arg.Do<SendGridMessage>(m => captured = m),
				Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body", false, "Custom Sender");

		// Act
		await sut.SendAsync(message);

		// Assert
		captured!.From.Name.Should().Be("Custom Sender");
	}

	[Fact]
	public async Task SendAsync_WithNullFromName_FallsBackToSettingsFromName()
	{
		// Arrange
		SendGridMessage? captured = null;
		_client.SendEmailAsync(
				Arg.Do<SendGridMessage>(m => captured = m),
				Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body", false, null);

		// Act
		await sut.SendAsync(message);

		// Assert
		captured!.From.Name.Should().Be(_settings.Value.FromName);
	}

	[Fact]
	public async Task SendAsync_PassesCancellationTokenToClient()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var token = cts.Token;

		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), token)
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body");

		// Act
		await sut.SendAsync(message, token);

		// Assert
		await _client.Received(1).SendEmailAsync(Arg.Any<SendGridMessage>(), token);
	}

	#endregion

	// -----------------------------------------------------------------------
	// SendAsync — non-success / error paths
	// -----------------------------------------------------------------------

	#region SendAsync Failures

	[Fact]
	public async Task SendAsync_WhenClientReturnsNonSuccess_ReturnsFailure()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.BadRequest, "Invalid request"));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body");

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Failure.Should().BeTrue();
	}

	[Fact]
	public async Task SendAsync_WhenClientReturnsNonSuccess_ErrorContainsStatusCode()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.TooManyRequests, "Rate limited"));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body");

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Error.Should().Contain("TooManyRequests");
	}

	[Fact]
	public async Task SendAsync_WhenClientReturnsNonSuccess_LogsError()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.InternalServerError, "Server error"));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body");

		// Act
		await sut.SendAsync(message);

		// Assert
		_logger.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception?>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task SendAsync_WhenClientThrowsException_ReturnsFailure()
	{
		// Arrange
		const string exceptionMessage = "Connection refused";
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException(exceptionMessage));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body");

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Failure.Should().BeTrue();
	}

	[Fact]
	public async Task SendAsync_WhenClientThrowsException_ErrorContainsExceptionMessage()
	{
		// Arrange
		const string exceptionMessage = "Network timeout occurred";
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Throws(new HttpRequestException(exceptionMessage));

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body");

		// Act
		var result = await sut.SendAsync(message);

		// Assert
		result.Error.Should().Contain(exceptionMessage);
	}

	[Fact]
	public async Task SendAsync_WhenClientThrowsException_LogsErrorWithException()
	{
		// Arrange
		var ex = new InvalidOperationException("SDK blew up");
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Throws(ex);

		var sut = CreateSut();
		var message = new EmailMessage("to@example.com", "Subject", "Body");

		// Act
		await sut.SendAsync(message);

		// Assert
		_logger.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Is<Exception?>(e => e != null && e.Message == ex.Message),
			Arg.Any<Func<object, Exception?, string>>());
	}

	#endregion

	// -----------------------------------------------------------------------
	// SendTemplatedAsync
	// -----------------------------------------------------------------------

	#region SendTemplatedAsync

	[Fact]
	public async Task SendTemplatedAsync_WhenClientSucceeds_ReturnsOk()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var model = new { Name = "Alice", IssueId = "ISSUE-42" };

		// Act
		var result = await sut.SendTemplatedAsync("IssueCreated", model, "alice@example.com");

		// Assert
		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task SendTemplatedAsync_DelegatesToSendAsync_InvokesClient()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var model = new { Key = "Value" };

		// Act
		await sut.SendTemplatedAsync("SomeTemplate", model, "user@example.com");

		// Assert
		await _client.Received(1).SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SendTemplatedAsync_WhenClientFails_PropagatesFailure()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.ServiceUnavailable, "Service down"));

		var sut = CreateSut();
		var model = new { Key = "Value" };

		// Act
		var result = await sut.SendTemplatedAsync("Template", model, "user@example.com");

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Failed to send email");
	}

	[Fact]
	public async Task SendTemplatedAsync_WhenClientThrows_PropagatesFailure()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Throws(new HttpRequestException("Connection reset"));

		var sut = CreateSut();
		var model = new { Key = "Value" };

		// Act
		var result = await sut.SendTemplatedAsync("Template", model, "user@example.com");

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Failed to send email");
	}

	[Fact]
	public async Task SendTemplatedAsync_LogsWarning_AboutUnimplementedTemplating()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var model = new { Key = "Value" };

		// Act
		await sut.SendTemplatedAsync("WelcomeEmail", model, "user@example.com");

		// Assert
		_logger.Received().Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Is<Exception?>(e => e == null),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task SendTemplatedAsync_WithCancellationToken_ForwardsTokenToClient()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var token = cts.Token;

		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), token)
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();
		var model = new { Key = "Value" };

		// Act
		await sut.SendTemplatedAsync("Template", model, "user@example.com", token);

		// Assert
		await _client.Received(1).SendEmailAsync(Arg.Any<SendGridMessage>(), token);
	}

	[Fact]
	public async Task SendTemplatedAsync_WithNullModel_HandlesGracefully()
	{
		// Arrange
		_client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
			.Returns(MakeResponse(HttpStatusCode.Accepted));

		var sut = CreateSut();

		// Act
		var result = await sut.SendTemplatedAsync<object?>("Template", null, "user@example.com");

		// Assert — null model serializes to "null"; the service should not crash
		result.Success.Should().BeTrue();
	}

	#endregion

	// -----------------------------------------------------------------------
	// Test models
	// -----------------------------------------------------------------------

	#region Test Models

	private sealed class IssueNotificationModel
	{
		public string IssueId { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string AssignedTo { get; set; } = string.Empty;
	}

	#endregion
}
