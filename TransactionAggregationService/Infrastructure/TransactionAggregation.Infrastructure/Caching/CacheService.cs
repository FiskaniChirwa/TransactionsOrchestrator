using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionAggregation.Models.Common;
using TransactionAggregation.Infrastructure.Configuration;
using TransactionAggregation.Infrastructure.Constants;

namespace TransactionAggregation.Infrastructure.Caching;

public class CacheService(
    IMemoryCache cache,
    ILogger<CacheService> logger,
    IOptions<MockProviderConfiguration> configuration) : ICacheService
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<CacheService> _logger = logger;
    private readonly MockProviderConfiguration _configuration = configuration.Value;

    public async Task<Result<T>> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<Result<T>>> fetchFunc,
        string serviceName) where T : class
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue<CachedData<T>>(cacheKey, out var cachedData))
            {
                var cacheAge = DateTime.UtcNow - cachedData!.CachedAt;
                var staleThreshold = TimeSpan.FromMinutes(_configuration.StaleThresholdMinutes);

                // Cache is fresh - return immediately
                if (cacheAge < staleThreshold)
                {
                    return Result<T>.SuccessResult(cachedData.Data);
                }

                // Cache is stale - return + background refresh
                _ = Task.Run(async () => await RefreshCacheAsync(cacheKey, fetchFunc, serviceName));

                return Result<T>.SuccessResultWithWarning(
                    cachedData.Data,
                    "Data may be slightly outdated, refreshing in background",
                    ApiConstants.WarningCodes.StaleCacheRefreshing);
            }

            // Cache miss - fetch from API
            _logger.LogInformation(
                "[{ServiceName}] Cache miss for '{CacheKey}'",
                serviceName,
                cacheKey);

            return await FetchAndCacheAsync(cacheKey, fetchFunc, serviceName);
        }
        catch (Exception ex)
        {
            // FALLBACK: Try to serve expired cache if fetch fails
            if (_cache.TryGetValue<CachedData<T>>(cacheKey, out var fallbackCache))
            {
                _logger.LogWarning(ex,
                    "[{ServiceName}] Serving fallback cache for '{CacheKey}' (age: {CacheAge})",
                    serviceName,
                    cacheKey,
                    DateTime.UtcNow - fallbackCache!.CachedAt);

                return Result<T>.SuccessResultWithWarning(
                    fallbackCache.Data,
                    "Serving cached data due to API unavailability",
                    ApiConstants.WarningCodes.FallbackCache);
            }

            // No cache at all - must return failure
            _logger.LogError(ex,
                "[{ServiceName}] Failed to fetch '{CacheKey}' with no cache available",
                serviceName,
                cacheKey);

            return Result<T>.FailureResult(
                "Unable to retrieve data and no cached version available",
                ApiConstants.ErrorCodes.NoCacheAvailable);
        }
    }

    private async Task<Result<T>> FetchAndCacheAsync<T>(
        string cacheKey,
        Func<Task<Result<T>>> fetchFunc,
        string serviceName) where T : class
    {
        var result = await fetchFunc();

        if (!result.Success)
        {
            return result;
        }

        // Cache the successful result
        var cachedData = new CachedData<T>
        {
            Data = result.Data!,
            CachedAt = DateTime.UtcNow
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_configuration.CacheExpirationMinutes)
        };

        _cache.Set(cacheKey, cachedData, cacheOptions);

        _logger.LogInformation(
            "[{ServiceName}] Fetched and cached '{CacheKey}'",
            serviceName,
            cacheKey);

        return result;
    }

    private async Task RefreshCacheAsync<T>(
        string cacheKey,
        Func<Task<Result<T>>> fetchFunc,
        string serviceName) where T : class
    {
        try
        {
            await FetchAndCacheAsync(cacheKey, fetchFunc, serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{ServiceName}] Background refresh failed for '{CacheKey}'",
                serviceName,
                cacheKey);

            // FALLBACK: Extend stale cache if refresh fails
            if (_cache.TryGetValue<CachedData<T>>(cacheKey, out var existingCache))
            {
                var cacheAge = DateTime.UtcNow - existingCache!.CachedAt;
                var maxStaleAge = TimeSpan.FromMinutes(_configuration.MaxStaleAgeMinutes);

                if (cacheAge < maxStaleAge)
                {
                    _logger.LogWarning(
                        "[{ServiceName}] Extending cache for '{CacheKey}' due to refresh failure (age: {Age})",
                        serviceName,
                        cacheKey,
                        cacheAge);

                    var extendedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_configuration.CacheExpirationMinutes)
                    };

                    _cache.Set(cacheKey, existingCache, extendedOptions);
                }
                else
                {
                    _logger.LogError(
                        "[{ServiceName}] Cache too old for '{CacheKey}' ({Age}), not extending",
                        serviceName,
                        cacheKey,
                        cacheAge);
                }
            }
        }
    }
}