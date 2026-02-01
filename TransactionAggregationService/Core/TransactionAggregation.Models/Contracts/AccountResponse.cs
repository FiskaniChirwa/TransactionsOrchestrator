namespace TransactionAggregation.Models.Contracts;

public class AccountResponse
{
    public long AccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OpenedDate { get; set; }
}