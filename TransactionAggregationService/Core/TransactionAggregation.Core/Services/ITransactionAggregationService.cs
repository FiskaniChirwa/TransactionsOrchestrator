using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Responses;

namespace TransactionAggregation.Core.Services;

public interface ITransactionAggregationService
{
    /// <summary>
    /// Gets all aggregated transaction data for a customer
    /// </summary>
    Task<Result<AggregatedTransactionResponse>> GetCustomerTransactionsAsync(
        long customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets transaction summary with category breakdowns
    /// </summary>
    Task<Result<TransactionSummaryResponse>> GetTransactionSummaryAsync(
        long customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets transaction statement
    /// </summary>
    Task<Result<StatementGenerationResponse>> GenerateTransactionStatementAsync(
        long customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null);
}