// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     EmailQueueBackgroundService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.Models;

namespace Web.Services;

/// <summary>
///   Background service that processes the email queue.
/// </summary>
public sealed class EmailQueueBackgroundService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<EmailQueueBackgroundService> _logger;
	private static readonly TimeSpan ProcessingInterval = TimeSpan.FromSeconds(10);

	public EmailQueueBackgroundService(
		IServiceProvider serviceProvider,
		ILogger<EmailQueueBackgroundService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Email Queue Background Service started");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await ProcessEmailQueueAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing email queue");
			}

			await Task.Delay(ProcessingInterval, stoppingToken);
		}

		_logger.LogInformation("Email Queue Background Service stopped");
	}

	private async Task ProcessEmailQueueAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var emailRepository = scope.ServiceProvider.GetRequiredService<IRepository<EmailQueueItem>>();
		var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

		// Get pending emails that are ready to be sent
		var pendingEmailsResult = await emailRepository.GetAllAsync(cancellationToken);
		if (pendingEmailsResult.Failure || pendingEmailsResult.Value is null)
		{
			return;
		}

		var emailsToProcess = pendingEmailsResult.Value
			.Where(e => e.Status == EmailQueueStatus.Pending &&
								 e.NextAttemptAt <= DateTime.UtcNow &&
								 e.Attempts < e.MaxAttempts)
			.OrderBy(e => e.QueuedAt)
			.Take(10) // Process up to 10 emails at a time
			.ToList();

		if (emailsToProcess.Count == 0)
		{
			return;
		}

		_logger.LogInformation("Processing {Count} emails from queue", emailsToProcess.Count);

		foreach (var email in emailsToProcess)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}

			await ProcessEmailAsync(email, emailRepository, emailService, cancellationToken);
		}
	}

	private async Task ProcessEmailAsync(
		EmailQueueItem email,
		IRepository<EmailQueueItem> emailRepository,
		IEmailService emailService,
		CancellationToken cancellationToken)
	{
		try
		{
			// Mark as sending
			email.Status = EmailQueueStatus.Sending;
			email.Attempts++;
			await emailRepository.UpdateAsync(email, cancellationToken);

			// Send the email
			var emailMessage = new EmailMessage(
				email.ToEmail,
				email.Subject,
				email.Body,
				email.IsHtml,
				email.FromName
			);

			var result = await emailService.SendAsync(emailMessage, cancellationToken);

			if (result.Success)
			{
				// Mark as sent
				email.Status = EmailQueueStatus.Sent;
				email.SentAt = DateTime.UtcNow;
				email.LastError = null;
				await emailRepository.UpdateAsync(email, cancellationToken);

				_logger.LogInformation("Email sent successfully to {ToEmail}", email.ToEmail);
			}
			else
			{
				// Mark as failed and schedule retry
				await HandleEmailFailureAsync(email, result.Error ?? "Unknown error", emailRepository, cancellationToken);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing email {EmailId}", email.Id);
			await HandleEmailFailureAsync(email, ex.Message, emailRepository, cancellationToken);
		}
	}

	private async Task HandleEmailFailureAsync(
		EmailQueueItem email,
		string error,
		IRepository<EmailQueueItem> emailRepository,
		CancellationToken cancellationToken)
	{
		email.LastError = error;

		if (email.Attempts >= email.MaxAttempts)
		{
			// Max attempts reached, mark as failed
			email.Status = EmailQueueStatus.Failed;
			_logger.LogError("Email {EmailId} failed after {Attempts} attempts: {Error}", email.Id, email.Attempts, error);
		}
		else
		{
			// Schedule retry with exponential backoff
			email.Status = EmailQueueStatus.Pending;
			var backoffMinutes = Math.Pow(2, email.Attempts - 1); // 1, 2, 4 minutes
			email.NextAttemptAt = DateTime.UtcNow.AddMinutes(backoffMinutes);
			_logger.LogWarning("Email {EmailId} failed (attempt {Attempts}/{MaxAttempts}). Retrying in {Minutes} minutes",
				email.Id, email.Attempts, email.MaxAttempts, backoffMinutes);
		}

		await emailRepository.UpdateAsync(email, cancellationToken);
	}
}
