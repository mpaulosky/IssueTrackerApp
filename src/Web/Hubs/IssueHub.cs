// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueHub.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Microsoft.AspNetCore.SignalR;

namespace Web.Hubs;

/// <summary>
///   SignalR hub for real-time issue notifications.
/// </summary>
public sealed class IssueHub : Hub
{
	private readonly ILogger<IssueHub> _logger;

	public IssueHub(ILogger<IssueHub> logger)
	{
		_logger = logger;
	}

	/// <summary>
	///   Called when a client connects to the hub.
	/// </summary>
	public override async Task OnConnectedAsync()
	{
		_logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
		
		// Add client to "all" group for broadcast notifications
		await Groups.AddToGroupAsync(Context.ConnectionId, "all");
		
		await base.OnConnectedAsync();
	}

	/// <summary>
	///   Called when a client disconnects from the hub.
	/// </summary>
	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (exception is not null)
		{
			_logger.LogError(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
		}
		else
		{
			_logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
		}

		await base.OnDisconnectedAsync(exception);
	}

	/// <summary>
	///   Joins a specific issue group for targeted notifications.
	/// </summary>
	/// <param name="issueId">The issue ID to subscribe to.</param>
	public async Task JoinIssueGroup(string issueId)
	{
		_logger.LogInformation("Client {ConnectionId} joining issue group: {IssueId}", Context.ConnectionId, issueId);
		await Groups.AddToGroupAsync(Context.ConnectionId, $"issue-{issueId}");
	}

	/// <summary>
	///   Leaves a specific issue group.
	/// </summary>
	/// <param name="issueId">The issue ID to unsubscribe from.</param>
	public async Task LeaveIssueGroup(string issueId)
	{
		_logger.LogInformation("Client {ConnectionId} leaving issue group: {IssueId}", Context.ConnectionId, issueId);
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"issue-{issueId}");
	}
}
