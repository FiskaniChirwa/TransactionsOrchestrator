using Document.Infrastructure.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Document.Api.HealthChecks;

public class DocumentStorageHealthCheck : IHealthCheck
{
    private readonly IStorageProvider _storageProvider;
    private readonly ILogger<DocumentStorageHealthCheck> _logger;

    public DocumentStorageHealthCheck(
        IStorageProvider storageProvider,
        ILogger<DocumentStorageHealthCheck> logger)
    {
        _storageProvider = storageProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testKey = "healthcheck/test.txt";
            var testData = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("health check"));

            await _storageProvider.StoreAsync(testKey, testData);
            var exists = await _storageProvider.ExistsAsync(testKey);
            await _storageProvider.DeleteAsync(testKey);

            if (exists)
            {
                return HealthCheckResult.Healthy("Storage is accessible and writable");
            }

            return HealthCheckResult.Degraded("Storage is accessible but test file verification failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed");
            return HealthCheckResult.Unhealthy(
                "Storage is not accessible",
                ex);
        }
    }
}