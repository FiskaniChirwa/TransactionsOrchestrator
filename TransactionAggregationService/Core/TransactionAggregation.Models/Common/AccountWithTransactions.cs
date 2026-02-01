using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Models.Common;

public class AccountWithTransactions
{
    public long AccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<TransactionResponse> Transactions { get; set; } = [];
}