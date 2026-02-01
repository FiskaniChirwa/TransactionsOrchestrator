namespace TransactionAggregation.Infrastructure.Constants;

public static class ApiConstants
{
    public static class ApiClientNames
    {
        public const string CustomerApi = "CustomerApi";
        public const string TransactionApi = "TransactionApi";
        public const string AccountApi = "AccountApi";
        public const string DocumentApi = "DocumentApi";
    }
    
    public static class ErrorCodes
    {
        public const string ApiUnavailable = "API_UNAVAILABLE";
        public const string ApiTimeout = "API_TIMEOUT";
        public const string ApiError = "API_ERROR";
        public const string EmptyResponse = "EMPTY_RESPONSE";
        public const string UnexpectedError = "UNEXPECTED_ERROR";
        public const string NoCacheAvailable = "NO_CACHE_AVAILABLE";
    }
    
    public static class WarningCodes
    {
        public const string StaleCacheRefreshing = "STALE_CACHE_REFRESHING";
        public const string FallbackCache = "FALLBACK_CACHE";
    }
    
    public static class CacheKeys
    {
        public static string Customer(long customerId) => $"customer_{customerId}";
        public static string CustomerAccounts(long customerId) => $"customer_accounts_{customerId}";
        public static string Transactions(long accountId, string fromDate, string toDate, int page, int pageSize) 
            => $"transactions_{accountId}_{fromDate}_{toDate}_{page}_{pageSize}";
        public static string AccountBalance(long accountId) => $"account_balance_{accountId}";
    }
}