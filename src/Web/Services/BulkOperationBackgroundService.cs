// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkOperationBackgroundService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using Domain.Abstractions;
using Domain.Features.Issues.Commands.Bulk;

using MediatR;

namespace Web.Services;

/// <summary>
///   Background service that processes queued bulk operations.
/// </summary>
public sealed class BulkOperationBackgroundService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly InMemoryBulkOperationQueue _queue;
	private readonly ILogger<BulkOperationBackgroundService> _logger;

	public BulkOperationBackgroundService(
		IServiceScopeFactory scopeFactory,
		InMemoryBulkOperationQueue queue,
		ILogger<BulkOperationBackgroundService> logger)
	{
		_scopeFactory = scopeFactory;
		_queue = queue;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Bulk operation background service started");

		await foreach (var operation in _queue.Reader.ReadAllAsync(stoppingToken))
		{
			try
			{
				_logger.LogInformation(
					"Processing queued bulk operation {OperationId} of type {Type}",
					operation.OperationId,
					operation.CommandType);

				await _queue.UpdateStatusAsync(
					operation.OperationId,
					BulkOperationStatus.Processing,
					null,
					stoppingToken);

				var result = await ProcessOperationAsync(operation, stoppingToken);

				await _queue.UpdateStatusAsync(
					operation.OperationId,
					result.FailureCount == result.TotalRequested
						? BulkOperationStatus.Failed
						: BulkOperationStatus.Completed,
					result,
					stoppingToken);

				_logger.LogInformation(
					"Completed bulk operation {OperationId}: {Success}/{Total} succeeded",
					operation.OperationId,
					result.SuccessCount,
					result.TotalRequested);
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Failed to process bulk operation {OperationId}",
					operation.OperationId);

				await _queue.UpdateStatusAsync(
					operation.OperationId,
					BulkOperationStatus.Failed,
					null,
					stoppingToken);
			}
		}
	}

	private async Task<BulkOperationResult> ProcessOperationAsync(
		QueuedBulkOperation operation,
		CancellationToken cancellationToken)
	{
		using var scope = _scopeFactory.CreateScope();

		return operation.Command switch
		{
			BulkUpdateStatusCommand statusCmd =>
				await ProcessStatusUpdateAsync(scope, statusCmd, cancellationToken),

			BulkUpdateCategoryCommand categoryCmd =>
				await ProcessCategoryUpdateAsync(scope, categoryCmd, cancellationToken),

			BulkAssignCommand assignCmd =>
				await ProcessAssignmentAsync(scope, assignCmd, cancellationToken),

			BulkDeleteCommand deleteCmd =>
				await ProcessDeleteAsync(scope, deleteCmd, cancellationToken),

			_ => throw new InvalidOperationException(
				$"Unknown command type: {operation.CommandType}")
		};
	}

	private async Task<BulkOperationResult> ProcessStatusUpdateAsync(
		IServiceScope scope,
		BulkUpdateStatusCommand command,
		CancellationToken cancellationToken)
	{
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var result = await mediator.Send(command, cancellationToken);
		return result.Value ?? new BulkOperationResult(command.IssueIds.Count, 0, command.IssueIds.Count, []);
	}

	private async Task<BulkOperationResult> ProcessCategoryUpdateAsync(
		IServiceScope scope,
		BulkUpdateCategoryCommand command,
		CancellationToken cancellationToken)
	{
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var result = await mediator.Send(command, cancellationToken);
		return result.Value ?? new BulkOperationResult(command.IssueIds.Count, 0, command.IssueIds.Count, []);
	}

	private async Task<BulkOperationResult> ProcessAssignmentAsync(
		IServiceScope scope,
		BulkAssignCommand command,
		CancellationToken cancellationToken)
	{
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var result = await mediator.Send(command, cancellationToken);
		return result.Value ?? new BulkOperationResult(command.IssueIds.Count, 0, command.IssueIds.Count, []);
	}

	private async Task<BulkOperationResult> ProcessDeleteAsync(
		IServiceScope scope,
		BulkDeleteCommand command,
		CancellationToken cancellationToken)
	{
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var result = await mediator.Send(command, cancellationToken);
		return result.Value ?? new BulkOperationResult(command.IssueIds.Count, 0, command.IssueIds.Count, []);
	}
}
