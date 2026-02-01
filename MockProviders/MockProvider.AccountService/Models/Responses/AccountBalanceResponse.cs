namespace MockProvider.AccountService.Models.Responses;

public class AccountBalanceResponse
{
    public long AccountId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}