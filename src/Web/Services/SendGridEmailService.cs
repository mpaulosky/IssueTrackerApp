// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SendGridEmailService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Web.Services;

/// <summary>
///   SendGrid-based email service implementation.
/// </summary>
public sealed class SendGridEmailService : IEmailService
{
	private readonly SendGridSettings _settings;
	private readonly ILogger<SendGridEmailService> _logger;
	private readonly SendGridClient _client;

	public SendGridEmailService(
		IOptions<SendGridSettings> settings,
		ILogger<SendGridEmailService> logger)
	{
		_settings = settings.Value;
		_logger = logger;
		_client = new SendGridClient(_settings.ApiKey);
	}

	public async Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default)
	{
		try
		{
			var from = new EmailAddress(_settings.FromEmail, message.FromName ?? _settings.FromName);
			var to = new EmailAddress(message.To);
			var msg = MailHelper.CreateSingleEmail(from, to, message.Subject, message.IsHtml ? null : message.Body, message.IsHtml ? message.Body : null);

			var response = await _client.SendEmailAsync(msg, ct);

			if (response.IsSuccessStatusCode)
			{
				_logger.LogInformation("Email sent successfully via SendGrid to {ToEmail}", message.To);
				return Result.Ok();
			}
			else
			{
				var body = await response.Body.ReadAsStringAsync(ct);
				_logger.LogError("SendGrid returned error status {StatusCode}: {Body}", response.StatusCode, body);
				return Result.Fail($"Failed to send email: {response.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send email via SendGrid to {ToEmail}", message.To);
			return Result.Fail($"Failed to send email: {ex.Message}");
		}
	}

	public Task<Result> SendTemplatedAsync<T>(string templateName, T model, string toEmail, CancellationToken ct = default)
	{
		// Simple template rendering - in production, use a proper template engine like RazorLight
		_logger.LogWarning("Template rendering not implemented for SendGrid service. Sending plain message.");

		var message = new EmailMessage(
			toEmail,
			$"Notification from {_settings.FromName}",
			$"Template: {templateName}\nModel: {System.Text.Json.JsonSerializer.Serialize(model)}",
			false
		);

		return SendAsync(message, ct);
	}
}
