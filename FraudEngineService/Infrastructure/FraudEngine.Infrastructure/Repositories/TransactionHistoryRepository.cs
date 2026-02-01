using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Infrastructure.Data;
using FraudEngine.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Repositories;

public class TransactionHistoryRepository(FraudEngineDbContext context) : ITransactionHistoryRepository
{
    private readonly FraudEngineDbContext _context = context;

    public async Task AddAsync(Transaction transaction)
    {
        var entity = transaction.ToEntity();

        _context.TransactionHistory.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountRecentTransactionsAsync(long customerId, string category, DateTime since)
    {
        return await _context.TransactionHistory
            .Where(t => t.CustomerId == customerId
                     && t.MerchantCategory == category
                     && t.TransactionDate >= since)
            .CountAsync();
    }

    public async Task<List<Transaction>> GetRecentTransactionsAsync(long customerId, int limit = 10)
    {
        var entities = await _context.TransactionHistory
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.TransactionDate)
            .Take(limit)
            .ToListAsync();

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<bool> IsFirstTimeCategoryAsync(long customerId, string category)
    {
        return !await _context.TransactionHistory
            .AnyAsync(t => t.CustomerId == customerId
                        && t.MerchantCategory == category);
    }
}