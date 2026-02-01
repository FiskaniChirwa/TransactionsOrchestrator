namespace TransactionAggregation.Infrastructure.Caching;

/// <summary>
/// Wrapper for cached data with timestamp
/// </summary>
public class CachedData<T> where T : class
{
    public T Data { get; set; } = null!;
    public DateTime CachedAt { get; set; }
}