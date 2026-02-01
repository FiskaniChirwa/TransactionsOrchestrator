namespace Document.Models.TemplateData;

public class AccountData
{
    public long AccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<TransactionData> Transactions { get; set; } = [];
}
