// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SignalRClientService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Microsoft.AspNetCore.SignalR.Client;
using Web.Services;

namespace Web.Services;

/// <summary>
/// Service for managing SignalR client connection and real-time updates.
/// </summary>
public sealed class SignalRClientService : IAsyncDisposable
{
	private readonly ILogger<SignalRClientService> _logger;
	private readonly ToastService _toastService;
	private readonly NavigationManager _navigationManager;
	private HubConnection? _hubConnection;
	private bool _isStarted;

	public SignalRClientService(
		ILogger<SignalRClientService> logger,
		ToastService toastService,
		NavigationManager navigationManager)
	{
		_logger = logger;
		_toastService = toastService;
		_navigationManager = navigationManager;
	}

	/// <summary>
	/// Gets the current connection state.
	/// </summary>
	public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

	/// <summary>
	/// Event fired when the connection state changes.
	/// </summary>
	public event Action<HubConnectionState>? OnConnectionStateChanged;

	/// <summary>
	/// Event fired when an issue is created.
	/// </summary>
	public event Action<string>? OnIssueCreated;

	/// <summary>
	/// Event fired when an issue is updated.
	/// </summary>
	public event Action<string>? OnIssueUpdated;

	/// <summary>
	/// Event fired when an issue is assigned.
	/// </summary>
	public event Action<string, string>? OnIssueAssigned;

	/// <summary>
	/// Event fired when a comment is added to an issue.
	/// </summary>
	public event Action<string>? OnCommentAdded;

	/// <summary>
	/// Event fired when an attachment is added to an issue.
	/// </summary>
	public event Action<string>? OnAttachmentAdded;

	/// <summary>
	/// Event fired when an attachment is deleted from an issue.
	/// </summary>
	public event Action<string>? OnAttachmentDeleted;

	/// <summary>
	/// Starts the SignalR connection.
	/// </summary>
	public async Task StartAsync()
	{
		if (_isStarted)
		{
			return;
		}

		try
		{
			var hubUrl = _navigationManager.ToAbsoluteUri("/hubs/issues");
			
			_hubConnection = new HubConnectionBuilder()
				.WithUrl(hubUrl)
				.WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
				.Build();

			RegisterHandlers();

			_hubConnection.Reconnecting += OnReconnecting;
			_hubConnection.Reconnected += OnReconnected;
			_hubConnection.Closed += OnClosed;

			await _hubConnection.StartAsync();
			_isStarted = true;
			
			NotifyStateChanged();
			_logger.LogInformation("SignalR connection established");
			_toastService.ShowSuccess("Connected to real-time updates");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to start SignalR connection");
			_toastService.ShowError("Failed to connect to real-time updates");
		}
	}

	/// <summary>
	/// Stops the SignalR connection.
	/// </summary>
	public async Task StopAsync()
	{
		if (_hubConnection is not null)
		{
			await _hubConnection.StopAsync();
			_isStarted = false;
			NotifyStateChanged();
		}
	}

	/// <summary>
	/// Joins a specific issue group for targeted notifications.
	/// </summary>
	public async Task JoinIssueGroupAsync(string issueId)
	{
		if (_hubConnection?.State == HubConnectionState.Connected)
		{
			await _hubConnection.InvokeAsync("JoinIssueGroup", issueId);
			_logger.LogInformation("Joined issue group: {IssueId}", issueId);
		}
	}

	/// <summary>
	/// Leaves a specific issue group.
	/// </summary>
	public async Task LeaveIssueGroupAsync(string issueId)
	{
		if (_hubConnection?.State == HubConnectionState.Connected)
		{
			await _hubConnection.InvokeAsync("LeaveIssueGroup", issueId);
			_logger.LogInformation("Left issue group: {IssueId}", issueId);
		}
	}

	private void RegisterHandlers()
	{
		if (_hubConnection is null) return;

		_hubConnection.On<string, string>("IssueCreated", (issueId, title) =>
		{
			_logger.LogInformation("Issue created: {IssueId} - {Title}", issueId, title);
			_toastService.ShowInfo($"New issue created: {title}");
			OnIssueCreated?.Invoke(issueId);
		});

		_hubConnection.On<string, string>("IssueUpdated", (issueId, title) =>
		{
			_logger.LogInformation("Issue updated: {IssueId} - {Title}", issueId, title);
			_toastService.ShowInfo($"Issue updated: {title}");
			OnIssueUpdated?.Invoke(issueId);
		});

		_hubConnection.On<string, string, string>("IssueAssigned", (issueId, assignedTo, title) =>
		{
			_logger.LogInformation("Issue assigned: {IssueId} to {AssignedTo}", issueId, assignedTo);
			_toastService.ShowInfo($"Issue assigned: {title}");
			OnIssueAssigned?.Invoke(issueId, assignedTo);
		});

		_hubConnection.On<string, string, string>("CommentAdded", (issueId, commentId, author) =>
		{
			_logger.LogInformation("Comment added to issue {IssueId} by {Author}", issueId, author);
			_toastService.ShowInfo($"New comment by {author}");
			OnCommentAdded?.Invoke(issueId);
		});

		_hubConnection.On<string, string, string>("AttachmentAdded", (issueId, attachmentId, fileName) =>
		{
			_logger.LogInformation("Attachment added to issue {IssueId}: {FileName}", issueId, fileName);
			_toastService.ShowInfo($"New attachment: {fileName}");
			OnAttachmentAdded?.Invoke(issueId);
		});

		_hubConnection.On<string, string>("AttachmentDeleted", (issueId, attachmentId) =>
		{
			_logger.LogInformation("Attachment deleted from issue {IssueId}: {AttachmentId}", issueId, attachmentId);
			_toastService.ShowInfo("Attachment deleted");
			OnAttachmentDeleted?.Invoke(issueId);
		});
	}

	private Task OnReconnecting(Exception? exception)
	{
		_logger.LogWarning(exception, "SignalR reconnecting...");
		_toastService.ShowWarning("Reconnecting to real-time updates...");
		NotifyStateChanged();
		return Task.CompletedTask;
	}

	private Task OnReconnected(string? connectionId)
	{
		_logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
		_toastService.ShowSuccess("Reconnected to real-time updates");
		NotifyStateChanged();
		return Task.CompletedTask;
	}

	private async Task OnClosed(Exception? exception)
	{
		_logger.LogWarning(exception, "SignalR connection closed");
		_toastService.ShowError("Disconnected from real-time updates");
		NotifyStateChanged();

		// Try to reconnect after a delay
		await Task.Delay(TimeSpan.FromSeconds(5));
		if (!_isStarted)
		{
			await StartAsync();
		}
	}

	private void NotifyStateChanged()
	{
		OnConnectionStateChanged?.Invoke(ConnectionState);
	}

	public async ValueTask DisposeAsync()
	{
		if (_hubConnection is not null)
		{
			_hubConnection.Reconnecting -= OnReconnecting;
			_hubConnection.Reconnected -= OnReconnected;
			_hubConnection.Closed -= OnClosed;
			
			await _hubConnection.DisposeAsync();
		}
	}
}
