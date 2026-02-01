namespace FraudEngine.Core.Models;

public class Transaction
{
    public long TransactionId { get; set; }
    public long CustomerId { get; set; }
    public long AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
}