// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SmtpEmailService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Web.Services;

/// <summary>
///   SMTP-based email service implementation.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
	private readonly SmtpSettings _settings;
	private readonly ILogger<SmtpEmailService> _logger;

	public SmtpEmailService(
		IOptions<SmtpSettings> settings,
		ILogger<SmtpEmailService> logger)
	{
		_settings = settings.Value;
		_logger = logger;
	}

	public async Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default)
	{
		try
		{
			using var smtpClient = new SmtpClient(_settings.Host, _settings.Port)
			{
				EnableSsl = _settings.EnableSsl,
				UseDefaultCredentials = false
			};

			if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
			{
				smtpClient.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
			}

			using var mailMessage = new MailMessage
			{
				From = new MailAddress(_settings.FromEmail, message.FromName ?? _settings.FromName),
				Subject = message.Subject,
				Body = message.Body,
				IsBodyHtml = message.IsHtml
			};

			mailMessage.To.Add(message.To);

			await smtpClient.SendMailAsync(mailMessage, ct);

			_logger.LogInformation("Email sent successfully via SMTP to {ToEmail}", message.To);

			return Result.Ok();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send email via SMTP to {ToEmail}", message.To);
			return Result.Fail($"Failed to send email: {ex.Message}");
		}
	}

	public Task<Result> SendTemplatedAsync<T>(string templateName, T model, string toEmail, CancellationToken ct = default)
	{
		// Simple template rendering - in production, use a proper template engine like RazorLight
		_logger.LogWarning("Template rendering not implemented for SMTP service. Sending plain message.");

		var message = new EmailMessage(
			toEmail,
			$"Notification from {_settings.FromName}",
			$"Template: {templateName}\nModel: {System.Text.Json.JsonSerializer.Serialize(model)}",
			false
		);

		return SendAsync(message, ct);
	}
}
