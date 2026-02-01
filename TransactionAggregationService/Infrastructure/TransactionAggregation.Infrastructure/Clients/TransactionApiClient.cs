using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionAggregation.Infrastructure.Caching;
using TransactionAggregation.Infrastructure.Configuration;
using TransactionAggregation.Infrastructure.Constants;
using TransactionAggregation.Infrastructure.Resilience;
using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Infrastructure.Clients;

public class TransactionApiClient : BaseApiClient, ITransactionApiClient
{
    public TransactionApiClient(
        HttpClient httpClient,
        ICacheService cacheService,
        ILogger<TransactionApiClient> logger,
        IOptions<MockProviderConfiguration> configuration,
        ResiliencePolicyFactory policyFactory)
        : base(httpClient, cacheService, logger, configuration, policyFactory, ApiConstants.ApiClientNames.TransactionApi)
    {
        HttpClient.BaseAddress = new Uri(Configuration.TransactionServiceUrl);
    }

    public async Task<Result<TransactionListResponse>> GetTransactionsAsync(
        long accountId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        var fromDateStr = fromDate?.ToString("yyyy-MM-dd") ?? "null";
        var toDateStr = toDate?.ToString("yyyy-MM-dd") ?? "null";
        var cacheKey = ApiConstants.CacheKeys.Transactions(accountId, fromDateStr, toDateStr, page, pageSize);

        return await CacheService.GetOrFetchAsync(
            cacheKey,
            () => FetchTransactionsAsync(accountId, fromDate, toDate, page, pageSize),
            ApiConstants.ApiClientNames.TransactionApi);
    }

    private async Task<Result<TransactionListResponse>> FetchTransactionsAsync(
        long accountId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        var queryParams = new List<string>
        {
            $"accountId={accountId}",
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (fromDate.HasValue)
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");

        if (toDate.HasValue)
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var queryString = string.Join("&", queryParams);
        var endpoint = $"/api/transactions?{queryString}";

        return await ExecuteWithResilienceAsync(
            ApiConstants.ApiClientNames.TransactionApi,
            () => HttpClient.GetAsync(endpoint),
            async response => (await response.Content.ReadFromJsonAsync<TransactionListResponse>())!,
            $"transactions for account {accountId}");
    }
}