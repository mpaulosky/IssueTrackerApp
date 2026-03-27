// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     IntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using FluentAssertions;

namespace AppHost.Tests;

/// <summary>
/// Integration tests for IssueTrackerApp web resource endpoints.
/// </summary>
public class IntegrationTests
{
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

	[Fact]
	public async Task WebHealthCheckReturnsOk()
	{
		// Arrange
		var appHost = await DistributedApplicationTestingBuilder
			.CreateAsync<Projects.AppHost>();

		appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
		{
			clientBuilder.AddStandardResilienceHandler();
		});

		await using var app = await appHost.BuildAsync();

		var resourceNotificationService = app.Services
			.GetRequiredService<ResourceNotificationService>();

		await app.StartAsync();

		// Act
		var httpClient = app.CreateHttpClient("web");

		await resourceNotificationService.WaitForResourceAsync(
			"web",
			KnownResourceStates.Running
		)
		.WaitAsync(DefaultTimeout);

		var response = await httpClient.GetAsync("/health");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task WebHomePageReturnsOk()
	{
		// Arrange
		var appHost = await DistributedApplicationTestingBuilder
			.CreateAsync<Projects.AppHost>();

		appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
		{
			clientBuilder.AddStandardResilienceHandler();
		});

		await using var app = await appHost.BuildAsync();

		var resourceNotificationService = app.Services
			.GetRequiredService<ResourceNotificationService>();

		await app.StartAsync();

		// Act
		var httpClient = app.CreateHttpClient("web");

		await resourceNotificationService.WaitForResourceAsync(
			"web",
			KnownResourceStates.Running
		)
		.WaitAsync(DefaultTimeout);

		var response = await httpClient.GetAsync("/");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}
}
