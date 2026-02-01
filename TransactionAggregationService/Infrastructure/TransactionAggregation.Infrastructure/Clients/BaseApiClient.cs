using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TransactionAggregation.Infrastructure.Caching;
using TransactionAggregation.Infrastructure.Configuration;
using TransactionAggregation.Infrastructure.Constants;
using TransactionAggregation.Infrastructure.Resilience;
using TransactionAggregation.Models.Common;

namespace TransactionAggregation.Infrastructure.Clients;

public abstract class BaseApiClient
{
    protected readonly HttpClient HttpClient;
    protected readonly ICacheService CacheService;
    protected readonly ILogger Logger;
    protected readonly MockProviderConfiguration Configuration;
    protected readonly IAsyncPolicy<HttpResponseMessage> ResiliencePolicy;

    protected BaseApiClient(
        HttpClient httpClient,
        ICacheService cacheService,
        ILogger logger,
        IOptions<MockProviderConfiguration> configuration,
        ResiliencePolicyFactory policyFactory,
        string serviceName)
    {
        HttpClient = httpClient;
        CacheService = cacheService;
        Logger = logger;
        Configuration = configuration.Value;
        ResiliencePolicy = policyFactory.CreatePolicy(serviceName);
    }

    protected async Task<Result<TResponse>> ExecuteWithResilienceAsync<TResponse>(
        string serviceName,
        Func<Task<HttpResponseMessage>> httpCall,
        Func<HttpResponseMessage, Task<TResponse>> parseResponse,
        string operationDescription) where TResponse : class
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await ResiliencePolicy.ExecuteAsync(httpCall);

            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError(
                    "[{ServiceName}] HTTP {StatusCode} after {Duration}ms - {Operation}",
                    serviceName,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    operationDescription);

                return Result<TResponse>.FailureResult(
                    $"External API returned {response.StatusCode}",
                    ApiConstants.ErrorCodes.ApiError);
            }

            var result = await parseResponse(response);

            if (result == null)
            {
                Logger.LogError(
                    "[{ServiceName}] Null response after {Duration}ms - {Operation}",
                    serviceName,
                    stopwatch.ElapsedMilliseconds,
                    operationDescription);

                return Result<TResponse>.FailureResult(
                    $"{operationDescription} returned null",
                    ApiConstants.ErrorCodes.EmptyResponse);
            }

            LogPerformance(serviceName, stopwatch.ElapsedMilliseconds, response.Content.Headers.ContentLength);

            return Result<TResponse>.SuccessResult(result);
        }
        catch (BrokenCircuitException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{ServiceName}] Circuit breaker open after {Duration}ms - {Operation}",
                serviceName,
                stopwatch.ElapsedMilliseconds,
                operationDescription);

            return Result<TResponse>.FailureResult(
                "External API is unavailable (circuit breaker is open)",
                ApiConstants.ErrorCodes.ApiUnavailable);
        }
        catch (TimeoutRejectedException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{ServiceName}] Request timed out after {Duration}ms - {Operation}",
                serviceName,
                stopwatch.ElapsedMilliseconds,
                operationDescription);

            return Result<TResponse>.FailureResult(
                "External API request timed out",
                ApiConstants.ErrorCodes.ApiTimeout);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{ServiceName}] HTTP request failed after {Duration}ms - {Operation}",
                serviceName,
                stopwatch.ElapsedMilliseconds,
                operationDescription);

            return Result<TResponse>.FailureResult(
                "External API is unavailable",
                ApiConstants.ErrorCodes.ApiUnavailable);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex,
                "[{ServiceName}] Unexpected error after {Duration}ms - {Operation}",
                serviceName,
                stopwatch.ElapsedMilliseconds,
                operationDescription);

            return Result<TResponse>.FailureResult(
                "An unexpected error occurred",
                ApiConstants.ErrorCodes.UnexpectedError);
        }
    }

    protected void LogPerformance(
        string serviceName,
        long elapsedMilliseconds,
        long? contentLength)
    {
        var responseSizeKb = contentLength.HasValue ? contentLength.Value / 1024.0 : 0;

        Logger.LogInformation(
            "[{ServiceName}] API call completed in {Duration}ms (Response size: ~{SizeKB:F2}KB)",
            serviceName,
            elapsedMilliseconds,
            responseSizeKb);

        if (elapsedMilliseconds > Configuration.SlowApiThresholdMs)
        {
            Logger.LogWarning(
                "[{ServiceName}] SLOW API: {Duration}ms (threshold: {Threshold}ms)",
                serviceName,
                elapsedMilliseconds,
                Configuration.SlowApiThresholdMs);
        }
    }
}