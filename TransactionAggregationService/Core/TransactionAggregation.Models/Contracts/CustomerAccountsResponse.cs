namespace TransactionAggregation.Models.Contracts;

public class CustomerAccountsResponse
{
    public long CustomerId { get; set; }
    public List<AccountResponse> Accounts { get; set; } = [];
}