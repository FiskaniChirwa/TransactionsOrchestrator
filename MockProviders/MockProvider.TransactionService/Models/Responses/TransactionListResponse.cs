namespace MockProvider.TransactionService.Models.Responses;

public class TransactionListResponse
{
    public long AccountId { get; set; }
    public List<TransactionResponse> Transactions { get; set; } = [];
    public PageInfo? PageInfo { get; set; }
}