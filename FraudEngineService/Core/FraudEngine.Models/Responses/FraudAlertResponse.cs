namespace FraudEngine.Models.Responses;

public class FraudAlertResponse
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public long CustomerId { get; set; }
    public long AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCategory { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public bool IsFraudulent { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> RulesTriggered { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}