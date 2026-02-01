using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using TransactionAggregation.Infrastructure.Clients;
using TransactionAggregation.Infrastructure.Configuration;
using TransactionAggregation.Infrastructure.Repositories;

namespace TransactionAggregation.Infrastructure.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxProcessorConfiguration _config;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        IOptions<OutboxProcessorConfiguration> config,
        ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox Processor started - Polling every {PollingInterval}s, Batch size: {BatchSize}, Parallel: {Parallel}",
            _config.PollingIntervalSeconds,
            _config.BatchSize,
            _config.EnableParallelProcessing);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_config.PollingIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("Outbox Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<OutboxRepository>();
        var FraudEngineApiClient = scope.ServiceProvider.GetRequiredService<IFraudEngineApiClient>();

        var messages = await outboxRepository.GetPendingAsync(_config.BatchSize);

        if (!messages.Any())
        {
            return;
        }

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        if (_config.EnableParallelProcessing)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism,
                CancellationToken = stoppingToken
            };

            await Parallel.ForEachAsync(messages, parallelOptions, async (message, ct) =>
            {
                await ProcessMessageAsync(message, FraudEngineApiClient, outboxRepository, ct);
            });
        }
        else
        {
            foreach (var message in messages)
            {
                await ProcessMessageAsync(message, FraudEngineApiClient, outboxRepository, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(
        Outbox.Models.OutboxMessage message,
        IFraudEngineApiClient FraudEngineApiClient,
        OutboxRepository outboxRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Processing outbox message: EventId={EventId}, Attempt={Attempt}",
                message.EventId,
                message.AttemptCount + 1);

            message.Status = "Processing";
            message.AttemptCount++;
            message.LastAttemptAt = DateTime.UtcNow;

            var result = await FraudEngineApiClient.SendEventAsync(
                message.EventId,
                message.Payload);

            if (result.Success)
            {
                message.Status = "Completed";
                message.ProcessedAt = DateTime.UtcNow;
                message.LastError = null;

                _logger.LogInformation(
                    "Successfully processed outbox message: EventId={EventId}",
                    message.EventId);
            }
            else
            {
                HandleFailure(
                    message,
                    errorMessage: result?.ErrorMessage ?? "Unknown Error",
                    errorCode: result?.ErrorCode ?? "GENERAL_FAILURE"
                );
            }

            await outboxRepository.UpdateAsync(message);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex,
                "Circuit breaker open - Message {EventId} remains pending",
                message.EventId);

            message.Status = "Pending";
            message.AttemptCount--;
            await outboxRepository.UpdateAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing message {EventId}",
                message.EventId);

            HandleFailure(message, ex.Message, "PROCESSING_ERROR");
            await outboxRepository.UpdateAsync(message);
        }
    }

    private void HandleFailure(Outbox.Models.OutboxMessage message, string errorMessage, string errorCode)
    {
        message.LastError = $"[{errorCode}] {errorMessage}";

        if (message.AttemptCount >= _config.MaxRetryAttempts)
        {
            message.Status = "Failed";

            _logger.LogError(
                "Message {EventId} FAILED after {Attempts} attempts - Error: {Error}",
                message.EventId,
                message.AttemptCount,
                message.LastError);
        }
        else
        {
            message.Status = "Pending";

            _logger.LogWarning(
                "Message {EventId} failed (attempt {Attempt}/{MaxAttempts}) - Will retry - Error: {Error}",
                message.EventId,
                message.AttemptCount,
                _config.MaxRetryAttempts,
                message.LastError);
        }
    }
}