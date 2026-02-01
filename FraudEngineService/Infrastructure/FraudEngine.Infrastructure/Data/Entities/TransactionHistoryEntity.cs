namespace FraudEngine.Infrastructure.Data.Entities;

public class TransactionHistoryEntity
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public long CustomerId { get; set; }
    public long AccountId { get; set; }
    public decimal Amount { get; set; }
    public string MerchantCategory { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime RecordedAt { get; set; }
}