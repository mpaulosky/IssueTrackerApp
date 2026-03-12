// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     QueueEmailCommand.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

using Domain.Abstractions;

namespace Domain.Features.Notifications;

/// <summary>
///   Command to queue an email for sending.
/// </summary>
public sealed record QueueEmailCommand : IRequest<Result>
{
	/// <summary>
	///   Recipient email address.
	/// </summary>
	public required string ToEmail { get; init; }

	/// <summary>
	///   Email subject.
	/// </summary>
	public required string Subject { get; init; }

	/// <summary>
	///   Email body (HTML or plain text).
	/// </summary>
	public required string Body { get; init; }

	/// <summary>
	///   Whether the body is HTML.
	/// </summary>
	public bool IsHtml { get; init; } = true;

	/// <summary>
	///   Optional sender name.
	/// </summary>
	public string? FromName { get; init; }
}

/// <summary>
///   Handler for queuing emails.
/// </summary>
public sealed class QueueEmailCommandHandler : IRequestHandler<QueueEmailCommand, Result>
{
	private readonly IRepository<EmailQueueItem> _emailQueueRepository;
	private readonly ILogger<QueueEmailCommandHandler> _logger;

	public QueueEmailCommandHandler(
		IRepository<EmailQueueItem> emailQueueRepository,
		ILogger<QueueEmailCommandHandler> logger)
	{
		_emailQueueRepository = emailQueueRepository;
		_logger = logger;
	}

	public async Task<Result> Handle(QueueEmailCommand request, CancellationToken cancellationToken)
	{
		try
		{
			var queueItem = new EmailQueueItem
			{
				ToEmail = request.ToEmail,
				Subject = request.Subject,
				Body = request.Body,
				IsHtml = request.IsHtml,
				FromName = request.FromName,
				QueuedAt = DateTime.UtcNow,
				NextAttemptAt = DateTime.UtcNow,
				Status = EmailQueueStatus.Pending
			};

			await _emailQueueRepository.AddAsync(queueItem, cancellationToken);

			_logger.LogInformation("Email queued for {ToEmail} with subject: {Subject}", request.ToEmail, request.Subject);

			return Result.Ok();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to queue email for {ToEmail}", request.ToEmail);
			return Result.Fail($"Failed to queue email: {ex.Message}");
		}
	}
}
