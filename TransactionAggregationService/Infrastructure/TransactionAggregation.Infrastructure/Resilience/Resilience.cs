using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TransactionAggregation.Infrastructure.Configuration;

namespace TransactionAggregation.Infrastructure.Resilience;

public class ResiliencePolicyFactory(
    IOptions<MockProviderConfiguration> configuration,
    ILogger<ResiliencePolicyFactory> logger)
{
    private readonly MockProviderConfiguration _configuration = configuration.Value;
    private readonly ILogger<ResiliencePolicyFactory> _logger = logger;

    public IAsyncPolicy<HttpResponseMessage> CreatePolicy(string serviceName)
    {
        // Timeout policy (innermost) - per request timeout
        var timeoutPolicy = Policy
            .TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(_configuration.TimeoutSeconds),
                TimeoutStrategy.Pessimistic);

        // Retry policy with exponential backoff - ONLY for transient failures
        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(r => r.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(
                _configuration.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "N/A";
                    _logger.LogWarning(
                        "[{ServiceName}] Retry attempt {RetryCount}/{MaxRetries} after {Delay:F1}s due to: {Exception} (Status: {StatusCode})",
                        serviceName,
                        retryCount,
                        _configuration.RetryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? "HTTP error",
                        statusCode);
                });

        // Circuit breaker policy (outermost) - ONLY for transient failures
        var circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(r => r.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _configuration.CircuitBreakerFailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(_configuration.CircuitBreakerDurationSeconds),
                onBreak: (outcome, duration) =>
                {
                    var errorMsg = outcome.Exception?.Message ??
                                   (outcome.Result != null ? $"HTTP {(int)outcome.Result.StatusCode}" : "Unknown error");
                    _logger.LogError(
                        "[{ServiceName}] Circuit breaker OPENED for {Duration:F0}s (threshold: {Threshold} failures) due to: {Error}",
                        serviceName,
                        duration.TotalSeconds,
                        _configuration.CircuitBreakerFailureThreshold,
                        errorMsg);
                },
                onReset: () =>
                {
                    _logger.LogInformation("[{ServiceName}] Circuit breaker RESET - connection restored", serviceName);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("[{ServiceName}] Circuit breaker HALF-OPEN - testing connection", serviceName);
                });

        // Wrap policies: Circuit Breaker → Retry → Timeout
        return circuitBreakerPolicy.WrapAsync(retryPolicy).WrapAsync(timeoutPolicy);
    }
}