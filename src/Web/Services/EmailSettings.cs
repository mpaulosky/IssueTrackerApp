// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     EmailSettings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

namespace Web.Services;

/// <summary>
///   Configuration for SendGrid email service.
/// </summary>
public sealed class SendGridSettings
{
	/// <summary>
	///   SendGrid API key.
	/// </summary>
	public string ApiKey { get; set; } = string.Empty;

	/// <summary>
	///   From email address.
	/// </summary>
	public string FromEmail { get; set; } = "noreply@issuetracker.com";

	/// <summary>
	///   From name to display.
	/// </summary>
	public string FromName { get; set; } = "IssueTracker";
}

/// <summary>
///   Configuration for SMTP email service.
/// </summary>
public sealed class SmtpSettings
{
	/// <summary>
	///   SMTP server host.
	/// </summary>
	public string Host { get; set; } = "localhost";

	/// <summary>
	///   SMTP server port.
	/// </summary>
	public int Port { get; set; } = 587;

	/// <summary>
	///   Username for SMTP authentication.
	/// </summary>
	public string? Username { get; set; }

	/// <summary>
	///   Password for SMTP authentication.
	/// </summary>
	public string? Password { get; set; }

	/// <summary>
	///   Whether to use SSL/TLS.
	/// </summary>
	public bool EnableSsl { get; set; } = true;

	/// <summary>
	///   From email address.
	/// </summary>
	public string FromEmail { get; set; } = "noreply@issuetracker.com";

	/// <summary>
	///   From name to display.
	/// </summary>
	public string FromName { get; set; } = "IssueTracker";
}
