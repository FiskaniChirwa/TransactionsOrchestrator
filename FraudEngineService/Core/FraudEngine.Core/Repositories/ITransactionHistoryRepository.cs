using FraudEngine.Core.Models;

namespace FraudEngine.Core.Repositories;

public interface ITransactionHistoryRepository
{
    Task AddAsync(Transaction transaction);
    Task<int> CountRecentTransactionsAsync(long customerId, string category, DateTime since);
    Task<List<Transaction>> GetRecentTransactionsAsync(long customerId, int limit = 10);
    Task<bool> IsFirstTimeCategoryAsync(long customerId, string category);
}