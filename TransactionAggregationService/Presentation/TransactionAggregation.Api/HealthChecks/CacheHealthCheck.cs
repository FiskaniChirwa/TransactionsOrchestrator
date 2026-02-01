using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TransactionAggregation.Api.HealthChecks;

public class CacheHealthCheck : IHealthCheck
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheHealthCheck> _logger;

    public CacheHealthCheck(
        IMemoryCache cache,
        ILogger<CacheHealthCheck> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test cache read/write
            var testKey = "__health_check_test__";
            var testValue = Guid.NewGuid().ToString();

            // Write to cache
            _cache.Set(testKey, testValue, TimeSpan.FromSeconds(1));

            // Read from cache
            if (_cache.TryGetValue(testKey, out string? cachedValue) && cachedValue == testValue)
            {
                // Clean up
                _cache.Remove(testKey);

                return Task.FromResult(HealthCheckResult.Healthy("Cache is operational"));
            }

            return Task.FromResult(
                HealthCheckResult.Degraded("Cache read/write test failed")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache health check failed");
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Cache is unavailable", ex)
            );
        }
    }
}