// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IEmailService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Abstractions;

/// <summary>
///   Service for sending email notifications.
/// </summary>
public interface IEmailService
{
	/// <summary>
	///   Sends an email message asynchronously.
	/// </summary>
	/// <param name="message">The email message to send.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Result indicating success or failure.</returns>
	Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default);

	/// <summary>
	///   Sends a templated email message asynchronously.
	/// </summary>
	/// <typeparam name="T">The type of the template model.</typeparam>
	/// <param name="templateName">The name of the template to use.</param>
	/// <param name="model">The model data for the template.</param>
	/// <param name="toEmail">The recipient email address.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Result indicating success or failure.</returns>
	Task<Result> SendTemplatedAsync<T>(string templateName, T model, string toEmail, CancellationToken ct = default);
}

/// <summary>
///   Represents an email message.
/// </summary>
public sealed record EmailMessage(
	string To,
	string Subject,
	string Body,
	bool IsHtml = true,
	string? FromName = null
);
