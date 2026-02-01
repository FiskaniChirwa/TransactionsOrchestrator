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

public class CustomerApiClient : BaseApiClient, ICustomerApiClient
{
    public CustomerApiClient(
        HttpClient httpClient,
        ICacheService cacheService,
        ILogger<CustomerApiClient> logger,
        IOptions<MockProviderConfiguration> configuration,
        ResiliencePolicyFactory policyFactory)
        : base(httpClient, cacheService, logger, configuration, policyFactory, ApiConstants.ApiClientNames.CustomerApi)
    {
        HttpClient.BaseAddress = new Uri(Configuration.CustomerServiceUrl);
    }

    public async Task<Result<CustomerResponse>> GetCustomerAsync(long customerId)
    {
        var cacheKey = $"customer_{customerId}";

        return await CacheService.GetOrFetchAsync(
            cacheKey,
            () => FetchCustomerAsync(customerId),
            ApiConstants.ApiClientNames.CustomerApi);
    }

    public async Task<Result<List<AccountResponse>>> GetCustomerAccountsAsync(long customerId)
    {
        var cacheKey = $"customer_accounts_{customerId}";

        var result = await CacheService.GetOrFetchAsync(
            cacheKey,
            () => FetchCustomerAccountsAsync(customerId),
            ApiConstants.ApiClientNames.CustomerApi);

        if (!result.Success)
        {
            return Result<List<AccountResponse>>.FailureResult(
                result.ErrorMessage!,
                result.ErrorCode!);
        }

        return Result<List<AccountResponse>>.SuccessResult(result.Data!.Accounts);
    }

    private async Task<Result<CustomerResponse>> FetchCustomerAsync(long customerId)
    {
        return await ExecuteWithResilienceAsync(
            ApiConstants.ApiClientNames.CustomerApi,
            () => HttpClient.GetAsync($"/api/customers/{customerId}"),
            async response => (await response.Content.ReadFromJsonAsync<CustomerResponse>())!,
            $"customer {customerId}");
    }

    private async Task<Result<CustomerAccountsResponse>> FetchCustomerAccountsAsync(long customerId)
    {
        return await ExecuteWithResilienceAsync(
            ApiConstants.ApiClientNames.CustomerApi,
            () => HttpClient.GetAsync($"/api/customers/{customerId}/accounts"),
            async response => (await response.Content.ReadFromJsonAsync<CustomerAccountsResponse>())!,
            $"accounts for customer {customerId}");
    }
}