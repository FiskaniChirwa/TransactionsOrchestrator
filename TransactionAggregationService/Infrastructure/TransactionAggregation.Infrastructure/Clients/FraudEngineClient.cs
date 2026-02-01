using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TransactionAggregation.Models.Common;
using TransactionAggregation.Infrastructure.Configuration;

namespace TransactionAggregation.Infrastructure.Clients;

public class FraudEngineApiClient : IFraudEngineApiClient
{
    private readonly HttpClient _httpClient;
    private readonly FraudEngineConfiguration _config;
    private readonly ILogger<FraudEngineApiClient> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public FraudEngineApiClient(
        HttpClient httpClient,
        IOptions<FraudEngineConfiguration> config,
        ILogger<FraudEngineApiClient> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _policy = CreateResiliencePolicy();
    }

    public async Task<Result> SendEventAsync(string eventId, string payload)
    {
        try
        {
            _logger.LogDebug("Sending event {EventId} to Fraud Engine", eventId);

            var response = await _policy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/fraud/analyze");
                request.Headers.Add("X-Event-Id", eventId);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                return await _httpClient.SendAsync(request);
            });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Event {EventId} successfully sent to Fraud Engine", eventId);
                return Result.SuccessResult();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Fraud Engine returned {StatusCode} for event {EventId}: {Error}",
                response.StatusCode,
                eventId,
                errorContent);

            return Result.Failure(
                $"Fraud Engine returned {response.StatusCode}",
                "FRAUD_ENGINE_ERROR");
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex,
                "Circuit breaker is open for event {EventId} - Fraud Engine unavailable",
                eventId);

            return Result.Failure(
                "Fraud Engine circuit breaker is open",
                "CIRCUIT_OPEN");
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogWarning(ex,
                "Timeout sending event {EventId} to Fraud Engine",
                eventId);

            return Result.Failure(
                "Fraud Engine request timed out",
                "TIMEOUT");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP error sending event {EventId} to Fraud Engine",
                eventId);

            return Result.Failure(
                $"HTTP error: {ex.Message}",
                "HTTP_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending event {EventId} to Fraud Engine",
                eventId);

            return Result.Failure(
                $"Unexpected error: {ex.Message}",
                "UNEXPECTED_ERROR");
        }
    }

    private IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy()
    {
        // Timeout policy (innermost)
        var timeoutPolicy = Policy
            .TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(_config.TimeoutSeconds),
                TimeoutStrategy.Pessimistic);

        // Retry policy with exponential backoff
        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(r => r.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(
                _config.RetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(_config.RetryDelaySeconds * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "[FraudEngine] Retry attempt {RetryCount}/{MaxRetries} after {Delay:F1}s",
                        retryCount,
                        _config.RetryAttempts,
                        timespan.TotalSeconds);
                });

        // Circuit breaker policy (outermost)
        var circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(r => r.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _config.CircuitBreakerFailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(_config.CircuitBreakerDurationSeconds),
                onBreak: (outcome, duration) =>
                {
                    _logger.LogError(
                        "[FraudEngine] Circuit breaker OPENED for {Duration:F0}s",
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("[FraudEngine] Circuit breaker RESET");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("[FraudEngine] Circuit breaker HALF-OPEN");
                });

        return circuitBreakerPolicy.WrapAsync(retryPolicy).WrapAsync(timeoutPolicy);
    }
}