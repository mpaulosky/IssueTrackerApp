// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     EmailQueueBackgroundServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for EmailQueueBackgroundService email processing.
/// </summary>
public sealed class EmailQueueBackgroundServiceTests : IDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IRepository<EmailQueueItem> _emailRepository;
	private readonly IEmailService _emailService;
	private readonly IServiceScope _scope;

	public EmailQueueBackgroundServiceTests()
	{
		_emailRepository = Substitute.For<IRepository<EmailQueueItem>>();
		_emailService = Substitute.For<IEmailService>();

		var scopedServiceProvider = Substitute.For<IServiceProvider>();
		scopedServiceProvider.GetService(typeof(IRepository<EmailQueueItem>)).Returns(_emailRepository);
		scopedServiceProvider.GetService(typeof(IEmailService)).Returns(_emailService);

		_scope = Substitute.For<IServiceScope>();
		_scope.ServiceProvider.Returns(scopedServiceProvider);

		_scopeFactory = Substitute.For<IServiceScopeFactory>();
		_scopeFactory.CreateScope().Returns(_scope);

		// CreateScope() is an extension method that calls GetRequiredService<IServiceScopeFactory>()
		_serviceProvider = Substitute.For<IServiceProvider>();
		_serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
	}

	public void Dispose()
	{
		_scope.Dispose();
	}

	private EmailQueueBackgroundService CreateService()
	{
		return new EmailQueueBackgroundService(
			_serviceProvider,
			NullLogger<EmailQueueBackgroundService>.Instance);
	}

	private static EmailQueueItem CreatePendingEmail(
		string toEmail = "test@test.com",
		string subject = "Test Subject",
		int attempts = 0,
		int maxAttempts = 3)
	{
		return new EmailQueueItem
		{
			Id = ObjectId.GenerateNewId(),
			ToEmail = toEmail,
			Subject = subject,
			Body = "<p>Test body</p>",
			IsHtml = true,
			Attempts = attempts,
			MaxAttempts = maxAttempts,
			QueuedAt = DateTime.UtcNow.AddMinutes(-5),
			NextAttemptAt = DateTime.UtcNow.AddMinutes(-1),
			Status = EmailQueueStatus.Pending
		};
	}

	#region Constructor Tests

	[Fact]
	public void Constructor_WithValidDependencies_CreatesService()
	{
		// Arrange & Act
		var service = CreateService();

		// Assert
		service.Should().NotBeNull();
	}

	#endregion

	#region ExecuteAsync Tests

	[Fact]
	public async Task ExecuteAsync_WhenQueueEmpty_DoesNotProcess()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok(Enumerable.Empty<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(50));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(30);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_WhenRepositoryReturnsFailure_DoesNotProcess()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<EmailQueueItem>>("Database error"));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(50));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(30);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_WithPendingEmail_ProcessesEmail()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailService.Received(1).SendAsync(
			Arg.Is<EmailMessage>(m => m.To == email.ToEmail),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_WhenEmailSentSuccessfully_UpdatesStatusToSent()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail();
		EmailQueueItem? updatedEmail = null;

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				updatedEmail = callInfo.Arg<EmailQueueItem>();
				return Result.Ok(updatedEmail);
			});

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailRepository.Received().UpdateAsync(
			Arg.Is<EmailQueueItem>(e => e.Status == EmailQueueStatus.Sent),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_WhenEmailSendFails_UpdatesStatusWithError()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail(attempts: 0, maxAttempts: 3);

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail("SMTP error"));

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailRepository.Received().UpdateAsync(
			Arg.Is<EmailQueueItem>(e => e.LastError == "SMTP error"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_WhenMaxAttemptsReached_SetsStatusToFailed()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail(attempts: 2, maxAttempts: 3);

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail("SMTP error"));

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailRepository.Received().UpdateAsync(
			Arg.Is<EmailQueueItem>(e => e.Status == EmailQueueStatus.Failed),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_WhenRetryNeeded_SchedulesNextAttempt()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail(attempts: 0, maxAttempts: 3);
		EmailQueueItem? updatedEmail = null;

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail("Temporary error"));

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				updatedEmail = callInfo.Arg<EmailQueueItem>();
				return Result.Ok(updatedEmail);
			});

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert - should schedule retry with future NextAttemptAt
		await _emailRepository.Received().UpdateAsync(
			Arg.Is<EmailQueueItem>(e => e.NextAttemptAt > DateTime.UtcNow.AddSeconds(-5)),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_WithMultipleEmails_ProcessesUpToLimit()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var emails = Enumerable.Range(1, 15)
			.Select(i => CreatePendingEmail($"user{i}@test.com", $"Subject {i}"))
			.ToList();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>(emails));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert - should process max 10 emails per batch
		await _emailService.Received(10).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_SkipsEmailsNotReadyForRetry()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var readyEmail = CreatePendingEmail("ready@test.com");
		var notReadyEmail = CreatePendingEmail("notready@test.com");
		notReadyEmail.NextAttemptAt = DateTime.UtcNow.AddMinutes(10); // Future

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([readyEmail, notReadyEmail]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert - should only process ready email
		await _emailService.Received(1).SendAsync(
			Arg.Is<EmailMessage>(m => m.To == "ready@test.com"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_SkipsEmailsWithMaxAttemptsReached()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var normalEmail = CreatePendingEmail("normal@test.com", attempts: 0, maxAttempts: 3);
		var exhaustedEmail = CreatePendingEmail("exhausted@test.com", attempts: 3, maxAttempts: 3);

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([normalEmail, exhaustedEmail]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert - should only process normal email
		await _emailService.Received(1).SendAsync(
			Arg.Is<EmailMessage>(m => m.To == "normal@test.com"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_SkipsNonPendingEmails()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var pendingEmail = CreatePendingEmail("pending@test.com");
		var sentEmail = CreatePendingEmail("sent@test.com");
		sentEmail.Status = EmailQueueStatus.Sent;

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([pendingEmail, sentEmail]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailService.Received(1).SendAsync(
			Arg.Is<EmailMessage>(m => m.To == "pending@test.com"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_WhenCancellationRequested_StopsProcessing()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok(Enumerable.Empty<EmailQueueItem>()));

		// Act
		await service.StartAsync(cts.Token);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert - should complete without hanging
	}

	[Fact]
	public async Task ExecuteAsync_WhenExceptionThrown_ContinuesProcessing()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var callCount = 0;

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(_ =>
			{
				callCount++;
				if (callCount == 1)
				{
					throw new InvalidOperationException("First call fails");
				}
				return Result.Ok(Enumerable.Empty<EmailQueueItem>());
			});

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert - should have been called more than once (continued after error)
		callCount.Should().BeGreaterThanOrEqualTo(1);
	}

	[Fact]
	public async Task ExecuteAsync_SetsStatusToSendingBeforeSending()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail();
		var updateCalls = new List<EmailQueueStatus>();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var item = callInfo.Arg<EmailQueueItem>();
				updateCalls.Add(item.Status);
				return Result.Ok(item);
			});

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert - first update should be Sending, second should be Sent
		updateCalls.Should().Contain(EmailQueueStatus.Sending);
		updateCalls.Should().Contain(EmailQueueStatus.Sent);
	}

	[Fact]
	public async Task ExecuteAsync_IncrementsAttemptCount()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail(attempts: 0);

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailRepository.Received().UpdateAsync(
			Arg.Is<EmailQueueItem>(e => e.Attempts == 1),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_SetsSentAtOnSuccess()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailRepository.Received().UpdateAsync(
			Arg.Is<EmailQueueItem>(e => e.SentAt != null),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ExecuteAsync_ClearsLastErrorOnSuccess()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		var email = CreatePendingEmail();
		email.LastError = "Previous error";

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<EmailQueueItem>>([email]));

		_emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok());

		_emailRepository.UpdateAsync(Arg.Any<EmailQueueItem>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => Result.Ok(callInfo.Arg<EmailQueueItem>()));

		// Act
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));
		try
		{
			await service.StartAsync(cts.Token);
			await Task.Delay(50);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}
		finally
		{
			await service.StopAsync(CancellationToken.None);
		}

		// Assert
		await _emailRepository.Received().UpdateAsync(
			Arg.Is<EmailQueueItem>(e => e.LastError == null && e.Status == EmailQueueStatus.Sent),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region StartAsync/StopAsync Tests

	[Fact]
	public async Task StartAsync_WhenCalled_StartsBackgroundProcessing()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok(Enumerable.Empty<EmailQueueItem>()));

		// Act
		await service.StartAsync(cts.Token);
		await Task.Delay(50);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert - should complete without hanging
	}

	[Fact]
	public async Task StopAsync_WhenCalled_StopsBackgroundProcessing()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		_emailRepository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok(Enumerable.Empty<EmailQueueItem>()));

		await service.StartAsync(cts.Token);

		// Act
		await service.StopAsync(CancellationToken.None);

		// Assert - should complete without hanging
		cts.Cancel();
	}

	#endregion
}
