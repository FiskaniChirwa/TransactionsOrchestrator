using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransactionAggregation.Infrastructure.Repositories;

namespace TransactionAggregation.Api.HealthChecks;

public class OutboxHealthCheck(
    OutboxRepository outboxRepository,
    ILogger<OutboxHealthCheck> logger) : IHealthCheck
{
    private readonly OutboxRepository _outboxRepository = outboxRepository;
    private readonly ILogger<OutboxHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var oldestPending = await _outboxRepository.GetOldestPendingAsync();
            var failedCount = await _outboxRepository.CountByStatusAsync("Failed");
            var pendingCount = await _outboxRepository.CountByStatusAsync("Pending");

            var data = new Dictionary<string, object>
            {
                { "pending_count", pendingCount },
                { "failed_count", failedCount }
            };

            // Check for old pending messages (> 5 minutes)
            if (oldestPending != null)
            {
                var age = DateTime.UtcNow - oldestPending.CreatedAt;
                data["oldest_pending_age_seconds"] = (int)age.TotalSeconds;

                if (age.TotalMinutes > 5)
                {
                    _logger.LogWarning(
                        "Outbox has messages pending for {Minutes:F1} minutes",
                        age.TotalMinutes);

                    return HealthCheckResult.Degraded(
                        $"Outbox processing lag: {age.TotalMinutes:F1} minutes",
                        data: data);
                }
            }

            // Check for too many failures
            if (failedCount > 10)
            {
                _logger.LogWarning(
                    "Outbox has {FailedCount} permanently failed messages",
                    failedCount);

                return HealthCheckResult.Degraded(
                    $"{failedCount} messages permanently failed",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Outbox processing is healthy",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outbox health check failed");

            return HealthCheckResult.Unhealthy(
                "Failed to check outbox status",
                ex);
        }
    }
}