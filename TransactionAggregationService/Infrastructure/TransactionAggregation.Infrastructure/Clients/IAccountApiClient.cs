using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Infrastructure.Clients;

public interface IAccountApiClient
{
    Task<Result<AccountBalanceResponse>> GetAccountBalanceAsync(long accountId);
}