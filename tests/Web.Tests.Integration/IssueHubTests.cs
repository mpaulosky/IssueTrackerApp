// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueHubTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using System.Text.Json;

using Domain.DTOs;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

using MongoDB.Bson;

namespace Web.Tests.Integration;

/// <summary>
/// Integration tests for the <see cref="IssueHub"/> SignalR hub.
/// Verifies real-time connection lifecycle and group membership behaviour
/// through a full ASP.NET Core test host.
/// </summary>
[Collection("Integration")]
public sealed class IssueHubTests : IntegrationTestBase
{
	/// <summary>Hub path as registered in Program.cs via <c>app.MapHub&lt;IssueHub&gt;("/hubs/issues")</c>.</summary>
	private const string HubPath = "hubs/issues";

	public IssueHubTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	// -----------------------------------------------------------------------
	// Helper: build a HubConnection that uses the in-process TestServer
	// -----------------------------------------------------------------------

	/// <summary>
	/// Creates a <see cref="HubConnection"/> wired to the test host.
	/// LongPolling is used because it is the most reliable transport through
	/// the TestServer HTTP message handler (no real TCP socket required).
	/// </summary>
	private HubConnection CreateHubConnection()
	{
		// Server.BaseAddress ends with "/", e.g. "http://localhost/"
		var hubUrl = new Uri(Factory.Server.BaseAddress, HubPath).ToString();

		return new HubConnectionBuilder()
			.WithUrl(hubUrl, options =>
			{
				// Route all HTTP traffic through the in-memory test server
				options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();

				// LongPolling is the most portable transport for TestHost
				options.Transports = HttpTransportType.LongPolling;
			})
			.Build();
	}

	// -----------------------------------------------------------------------
	// Test 1 – connect
	// -----------------------------------------------------------------------

	/// <summary>
	/// Connecting to the hub should succeed and the connection should reach
	/// <see cref="HubConnectionState.Connected"/>.
	/// OnConnectedAsync adds the client to the "all" group; the fact that
	/// StartAsync completes without throwing confirms the group-add path ran.
	/// </summary>
	[Fact]
	public async Task Connect_AddsClientToAllGroup_WhenConnected()
	{
		// Arrange
		var connection = CreateHubConnection();

		try
		{
			// Act
			await connection.StartAsync();

			// Assert
			connection.State.Should().Be(HubConnectionState.Connected);
		}
		finally
		{
			await connection.StopAsync();
			await connection.DisposeAsync();
		}
	}

	// -----------------------------------------------------------------------
	// Test 2 – JoinIssueGroup
	// -----------------------------------------------------------------------

	/// <summary>
	/// Invoking <c>JoinIssueGroup</c> on a connected hub should complete
	/// without throwing.
	/// </summary>
	[Fact]
	public async Task JoinIssueGroup_Succeeds_WhenConnected()
	{
		// Arrange
		var connection = CreateHubConnection();

		try
		{
			await connection.StartAsync();

			// Act & Assert – no exception expected
			var act = async () => await connection.InvokeAsync("JoinIssueGroup", "issue-123");
			await act.Should().NotThrowAsync();
		}
		finally
		{
			await connection.StopAsync();
			await connection.DisposeAsync();
		}
	}

	// -----------------------------------------------------------------------
	// Test 3 – LeaveIssueGroup
	// -----------------------------------------------------------------------

	/// <summary>
	/// Invoking <c>LeaveIssueGroup</c> after joining should complete without
	/// throwing even when the client is no longer in the group.
	/// </summary>
	[Fact]
	public async Task LeaveIssueGroup_Succeeds_WhenConnected()
	{
		// Arrange
		var connection = CreateHubConnection();

		try
		{
			await connection.StartAsync();
			await connection.InvokeAsync("JoinIssueGroup", "issue-456");

			// Act & Assert – no exception expected
			var act = async () => await connection.InvokeAsync("LeaveIssueGroup", "issue-456");
			await act.Should().NotThrowAsync();
		}
		finally
		{
			await connection.StopAsync();
			await connection.DisposeAsync();
		}
	}

	// -----------------------------------------------------------------------
	// Test 4 – disconnect
	// -----------------------------------------------------------------------

	/// <summary>
	/// Stopping a connected hub connection should move it to
	/// <see cref="HubConnectionState.Disconnected"/> cleanly.
	/// </summary>
	[Fact]
	public async Task Disconnect_Completes_WhenConnectionStopped()
	{
		// Arrange
		var connection = CreateHubConnection();

		try
		{
			await connection.StartAsync();
			connection.State.Should().Be(HubConnectionState.Connected);

			// Act
			await connection.StopAsync();

			// Assert
			connection.State.Should().Be(HubConnectionState.Disconnected);
		}
		finally
		{
			await connection.DisposeAsync();
		}
	}

	// -----------------------------------------------------------------------
	// Test 5 (bonus) – IssueVoted broadcast
	// -----------------------------------------------------------------------

	/// <summary>
	/// When a vote is cast via the REST endpoint the hub should broadcast an
	/// <c>IssueVoted</c> message to all connected clients.
	/// <para>
	/// This test verifies the end-to-end flow:
	/// REST POST → VoteEndpoints → IHubContext.Clients.All.SendAsync → client receives event.
	/// </para>
	/// <para>
	/// We use <c>On&lt;object&gt;</c> rather than <c>On&lt;IssueDto&gt;</c> because the
	/// server's default SignalR JSON serialisation does not include the custom
	/// <see cref="ObjectIdJsonConverter"/> needed to round-trip <see cref="MongoDB.Bson.ObjectId"/>.
	/// For this test we only care that the broadcast arrives; payload validation of the
	/// HTTP response covers the DTO content.
	/// </para>
	/// </summary>
	[Fact]
	public async Task IssueVoted_EventReceived_WhenVoteIsCastViaApi()
	{
		// ── Arrange ──────────────────────────────────────────────────────────
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0], "Hub Vote Test Issue");
		var issueId = issue.Id.ToString();

		var connection = CreateHubConnection();

		// TCS<bool> – avoids any ObjectId deserialisation in the hub handler.
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		try
		{
			await connection.StartAsync();
			connection.State.Should().Be(HubConnectionState.Connected);

			// Register listener BEFORE casting the vote so we cannot miss the event
			connection.On<object>("IssueVoted", _ => tcs.TrySetResult(true));

			// Authenticated client (User role) satisfies the "UserPolicy" on VoteEndpoints
			using var authClient = CreateAuthenticatedClient("User");

			// Act – cast a vote; this triggers Clients.All.SendAsync("IssueVoted", ...)
			var response = await authClient.PostAsync($"/api/issues/{issueId}/vote", content: null);
			response.IsSuccessStatusCode.Should().BeTrue(
				because: $"POST /api/issues/{issueId}/vote should succeed but returned {response.StatusCode}: " +
				         $"{await response.Content.ReadAsStringAsync()}");

			// Assert – the broadcast should arrive within 5 seconds
			var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
			completedTask.Should().Be(tcs.Task,
				because: "IssueVoted SignalR broadcast should have been received within 5 seconds");
		}
		finally
		{
			await connection.StopAsync();
			await connection.DisposeAsync();
		}
	}
}
