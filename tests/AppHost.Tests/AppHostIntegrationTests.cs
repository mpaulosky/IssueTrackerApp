// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHostIntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using FluentAssertions;

namespace AppHost.Tests;

/// <summary>
/// Integration tests for the IssueTrackerApp AppHost configuration and resources.
/// </summary>
public class AppHostIntegrationTests
{
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

	[Fact]
	public async Task WebResourceHealthCheckReturnsOk()
	{
		// Arrange
		var cancellationToken = CancellationToken.None;
		var appHost = await DistributedApplicationTestingBuilder
			.CreateAsync<Projects.AppHost>(cancellationToken);

		appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
		{
			clientBuilder.AddStandardResilienceHandler();
		});

		await using var app = await appHost.BuildAsync(cancellationToken)
			.WaitAsync(DefaultTimeout, cancellationToken);
		await app.StartAsync(cancellationToken)
			.WaitAsync(DefaultTimeout, cancellationToken);

		// Act
		var httpClient = app.CreateHttpClient("web");
		await app.ResourceNotifications
			.WaitForResourceHealthyAsync("web", cancellationToken)
			.WaitAsync(DefaultTimeout, cancellationToken);
		using var response = await httpClient.GetAsync("/health", cancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}
}
