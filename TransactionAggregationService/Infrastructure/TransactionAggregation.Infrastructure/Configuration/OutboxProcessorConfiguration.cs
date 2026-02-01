namespace TransactionAggregation.Infrastructure.Configuration;

public class OutboxProcessorConfiguration
{
    public int PollingIntervalSeconds { get; set; } = 1;
    public int BatchSize { get; set; } = 50;
    public int MaxRetryAttempts { get; set; } = 10;
    public bool EnableParallelProcessing { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = 10;
}