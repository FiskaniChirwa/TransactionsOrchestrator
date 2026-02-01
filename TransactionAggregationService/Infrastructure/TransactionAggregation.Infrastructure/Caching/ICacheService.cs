using TransactionAggregation.Models.Common;

namespace TransactionAggregation.Infrastructure.Caching;

/// <summary>
/// Stale-While-Revalidate caching service with fallback support
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get data from cache with SWR pattern, or fetch fresh data
    /// </summary>
    /// <typeparam name="T">Type of cached data</typeparam>
    /// <param name="cacheKey">Unique cache key</param>
    /// <param name="fetchFunc">Function to fetch fresh data when needed</param>
    /// <param name="serviceName">Name of the service (for logging)</param>
    /// <returns>Cached or fresh data</returns>
    Task<Result<T>> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<Result<T>>> fetchFunc,
        string serviceName) where T : class;
}