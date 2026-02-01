namespace Document.Models.TemplateData;

public class TransactionStatementData
{
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<AccountData> Accounts { get; set; } = [];
    public int TotalTransactions { get; set; }
    public DateRangeData? DateRange { get; set; }
}
