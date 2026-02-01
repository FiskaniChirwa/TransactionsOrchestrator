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

public class AccountApiClient : BaseApiClient, IAccountApiClient
{
    public AccountApiClient(
        HttpClient httpClient,
        ICacheService cacheService,
        ILogger<AccountApiClient> logger,
        IOptions<MockProviderConfiguration> configuration,
        ResiliencePolicyFactory policyFactory)
        : base(httpClient, cacheService, logger, configuration, policyFactory, ApiConstants.ApiClientNames.AccountApi)
    {
        HttpClient.BaseAddress = new Uri(Configuration.AccountServiceUrl);
    }

    public async Task<Result<AccountBalanceResponse>> GetAccountBalanceAsync(long accountId)
    {
        var cacheKey = ApiConstants.CacheKeys.AccountBalance(accountId);

        return await CacheService.GetOrFetchAsync(
            cacheKey,
            () => FetchAccountBalanceAsync(accountId),
            ApiConstants.ApiClientNames.AccountApi);
    }

    private async Task<Result<AccountBalanceResponse>> FetchAccountBalanceAsync(long accountId)
    {
        return await ExecuteWithResilienceAsync(
            ApiConstants.ApiClientNames.AccountApi,
            () => HttpClient.GetAsync($"/api/accounts/{accountId}/balance"),
            async response => (await response.Content.ReadFromJsonAsync<AccountBalanceResponse>())!,
            $"balance for account {accountId}");
    }
}