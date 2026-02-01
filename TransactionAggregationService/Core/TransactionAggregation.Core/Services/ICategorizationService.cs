using TransactionAggregation.Models.Common;
using TransactionAggregation.Models.Contracts;

namespace TransactionAggregation.Core.Services;

public interface ICategorizationService
{
    /// <summary>
    /// Groups transactions by category and calculates summary statistics
    /// </summary>
    List<CategorySummary> GroupTransactionsByCategory(List<TransactionResponse> transactions);

    /// <summary>
    /// Groups transactions by category (returns dictionary for flexible access)
    /// </summary>
    Dictionary<string, List<TransactionResponse>> GroupByCategory(List<TransactionResponse> transactions);

    /// <summary>
    /// Calculates total amount for a specific category
    /// </summary>
    decimal CalculateTotalForCategory(string category, List<TransactionResponse> transactions);

    /// <summary>
    /// Categorizes a single transaction based on MCC code and merchant name
    /// </summary>
    string CategorizeTransaction(TransactionResponse transaction);
}