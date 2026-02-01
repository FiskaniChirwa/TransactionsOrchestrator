using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TransactionAggregation.Infrastructure.Configuration;

namespace TransactionAggregation.Api.HealthChecks;

public class ExternalApiHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<MockProviderConfiguration> configuration,
    ILogger<ExternalApiHealthCheck> logger) : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly MockProviderConfiguration _configuration = configuration.Value;
    private readonly ILogger<ExternalApiHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var checks = new Dictionary<string, (string Url, string TestEndpoint)>
        {
            { "CustomerService", (_configuration.CustomerServiceUrl, "/api/customers/1001") },
            { "TransactionService", (_configuration.TransactionServiceUrl, "/api/transactions?accountId=10000&pageSize=1") },
            { "AccountService", (_configuration.AccountServiceUrl, "/api/accounts/10000/balance") }
        };

        var results = new Dictionary<string, string>();
        var allHealthy = true;

        foreach (var (serviceName, (baseUrl, testEndpoint)) in checks)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(2);

                // Just call a real endpoint to check if service responds
                var response = await client.GetAsync($"{baseUrl}{testEndpoint}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    results[serviceName] = "Healthy";
                }
                else
                {
                    results[serviceName] = $"Unhealthy (HTTP {response.StatusCode})";
                    allHealthy = false;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Health check failed for {ServiceName}", serviceName);
                results[serviceName] = "Unavailable";
                allHealthy = false;
            }
            catch (TaskCanceledException)
            {
                results[serviceName] = "Timeout";
                allHealthy = false;
            }
        }

        var status = allHealthy ? HealthStatus.Healthy : HealthStatus.Degraded;
        var description = allHealthy
            ? "All external services are healthy"
            : "Some external services are unavailable";

        return new HealthCheckResult(
            status,
            description,
            data: results.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        );
    }
}