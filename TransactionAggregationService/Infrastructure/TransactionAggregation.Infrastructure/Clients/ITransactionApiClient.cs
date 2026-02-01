using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Infrastructure.Clients;

public interface ITransactionApiClient
{
    Task<Result<TransactionListResponse>> GetTransactionsAsync(
        long accountId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50);
}