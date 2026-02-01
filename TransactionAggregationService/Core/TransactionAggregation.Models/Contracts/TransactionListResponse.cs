using TransactionAggregation.Models.Common;

namespace TransactionAggregation.Models.Contracts;

public class TransactionListResponse
{
    public long AccountId { get; set; }
    public List<TransactionResponse> Transactions { get; set; } = [];
    public PageInfo? PageInfo { get; set; }
}