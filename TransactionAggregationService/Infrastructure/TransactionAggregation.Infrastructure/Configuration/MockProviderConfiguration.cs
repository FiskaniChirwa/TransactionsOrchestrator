namespace TransactionAggregation.Infrastructure.Configuration;

public class MockProviderConfiguration
{
    public string CustomerServiceUrl { get; set; } = string.Empty;
    public string TransactionServiceUrl { get; set; } = string.Empty;
    public string AccountServiceUrl { get; set; } = string.Empty;
    
    // Resilience settings
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 2;
    public int CircuitBreakerFailureThreshold { get; set; } = 3;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
    
    // Cache settings
    public double StaleThresholdMinutes { get; set; } = 0.5; // 30 seconds
    public double CacheExpirationMinutes { get; set; } = 5;
    public double MaxStaleAgeMinutes { get; set; } = 30;

    // Performance settings
    public int SlowApiThresholdMs { get; set; } = 3000; 
}