namespace FraudEngine.Infrastructure.Data.Entities;

public class ProcessedEventEntity
{
    public long Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public long TransactionId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Result { get; set; } = string.Empty;
}