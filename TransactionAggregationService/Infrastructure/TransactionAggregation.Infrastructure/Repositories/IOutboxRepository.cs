using TransactionAggregation.Infrastructure.Outbox.Models;

namespace TransactionAggregation.Infrastructure.Repositories;

public interface IOutboxRepository
{
    Task<OutboxMessage> AddAsync(OutboxMessage message);
    Task AddBatchAsync(List<OutboxMessage> messages);
    Task<List<OutboxMessage>> GetPendingAsync(int batchSize);
    Task UpdateAsync(OutboxMessage message);
    Task<int> CountByStatusAsync(string status);
    Task<OutboxMessage?> GetOldestPendingAsync();
    Task<OutboxMessage?> GetByEventIdAsync(string eventId);
}