using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TransactionAggregation.Infrastructure.Configuration;
using TransactionAggregation.Infrastructure.Constants;
using TransactionAggregation.Infrastructure.Resilience;
using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Infrastructure.Clients;

public class DocumentApiClient : IDocumentApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentApiClient> _logger;
    private readonly DocumentConfiguration _config;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public DocumentApiClient(
        HttpClient httpClient,
        ILogger<DocumentApiClient> logger,
        IOptions<DocumentConfiguration> config,
        ResiliencePolicyFactory policyFactory)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
        
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        
        _resiliencePolicy = policyFactory.CreatePolicy(ApiConstants.ApiClientNames.DocumentApi);
    }

    public async Task<Result<GenerateDocumentResponse>> GenerateDocumentAsync(GenerateDocumentRequest request)
    {
        var operation = $"generate statement for customer {request.Data["customerId"]}";
        
        return await ExecuteWithResilienceAsync(
            () => _httpClient.PostAsJsonAsync("/api/documents/generate", request),
            async response => (await response.Content.ReadFromJsonAsync<GenerateDocumentResponse>())!,
            operation);
    }

    private async Task<Result<T>> ExecuteWithResilienceAsync<T>(
        Func<Task<HttpResponseMessage>> httpCall,
        Func<HttpResponseMessage, Task<T>> parseResponse,
        string operationDescription) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var serviceName = ApiConstants.ApiClientNames.DocumentApi;

        try
        {
            var response = await _resiliencePolicy.ExecuteAsync(httpCall);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[{ServiceName}] HTTP {StatusCode} - {Operation}", 
                    serviceName, (int)response.StatusCode, operationDescription);

                return Result<T>.FailureResult(
                    $"Document service returned {response.StatusCode}", 
                    "DOCUMENT_SERVICE_ERROR");
            }

            var result = await parseResponse(response);
            LogPerformance(stopwatch.ElapsedMilliseconds, response.Content.Headers.ContentLength);

            return result != null 
                ? Result<T>.SuccessResult(result)
                : Result<T>.FailureResult("Empty response from service", "EMPTY_RESPONSE");
        }
        catch (Exception ex) when (ex is BrokenCircuitException or TimeoutRejectedException or HttpRequestException)
        {
            stopwatch.Stop();
            var errorCode = ex switch {
                BrokenCircuitException => "CIRCUIT_OPEN",
                TimeoutRejectedException => "TIMEOUT",
                _ => "NETWORK_ERROR"
            };

            _logger.LogError(ex, "[{ServiceName}] Failed during {Operation}", serviceName, operationDescription);
            return Result<T>.FailureResult($"Service unavailable: {ex.Message}", errorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in DocumentApiClient");
            return Result<T>.FailureResult("An unexpected error occurred", "UNEXPECTED_ERROR");
        }
    }

    private void LogPerformance(long elapsedMs, long? size)
    {
        _logger.LogInformation("[DocumentApi] Call completed in {Duration}ms", elapsedMs);
        
        if (elapsedMs > 2000)
        {
            _logger.LogWarning("[DocumentApi] SLOW API detected: {Duration}ms", elapsedMs);
        }
    }
}